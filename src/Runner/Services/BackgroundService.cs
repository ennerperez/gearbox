using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Gearbox.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Gearbox.Runner.Services
{
    public partial class BackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly IBackend _backend;
        private readonly IBrowserService _browserService;
        private readonly ILogger<BackgroundService> _logger;
        private readonly IQueueService _queueService;

        public BackgroundService(IBackend backend, IBrowserService browserService, IQueueService queueService, ILogger<BackgroundService> logger)
        {
            _backend = backend;
            _browserService = browserService;
            _logger = logger;
            _queueService = queueService;
        }

        private static readonly string[] s_registerCommand = ["--register", "-r"];
        private static readonly string[] s_unregisterCommand = ["--unregister", "-u"];
        private static readonly string[] s_forceSourceCommand = ["--forceSource", "-fS"];

        [GeneratedRegex("\\-r|\\-\\-register", RegexOptions.Compiled)]
        private static partial Regex RegisterCommandRegex();

        [GeneratedRegex("\\-u|\\-\\-unregister", RegexOptions.Compiled)]
        private static partial Regex UnregisterCommandRegex();

        [GeneratedRegex("(.*) [\\-fS|\\-\\-forceSource]{1,} (.*)", RegexOptions.Compiled)]
        private static partial Regex ForceSourceCommandRegex();

        public static readonly string[] Commands = s_registerCommand.Concat(s_unregisterCommand).ToArray();

        private static readonly CancellationTokenSource s_cancellationTokenSource = new();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested && !s_cancellationTokenSource.IsCancellationRequested)
            {
                var count = await _queueService.PeekMessagesAsync(Metadata.Product ?? "Gearbox");
                var cmd = await _queueService.ReceiveMessageAsync(Metadata.Product ?? "Gearbox");
                if (cmd != null && !string.IsNullOrWhiteSpace(cmd.MessageText))
                {
                    var forceRegisterCommandMatch = RegisterCommandRegex().Match(cmd.MessageText);
                    var forceUnregisterSourceCommandMatch = UnregisterCommandRegex().Match(cmd.MessageText);

                    if (forceRegisterCommandMatch.Success)
                    {
                        await _backend.RegisterAsync();
                        await StopAsync(stoppingToken);
                        await s_cancellationTokenSource.CancelAsync();
                    }
                    else if (forceUnregisterSourceCommandMatch.Success)
                    {
                        await _backend.UnregisterAsync();
                        await StopAsync(stoppingToken);
                        await s_cancellationTokenSource.CancelAsync();
                    }
                    else
                    {
                        var forceSourceCommandMatch = ForceSourceCommandRegex().Match(cmd.MessageText);
                        var windowTitle = forceSourceCommandMatch.Success ? forceSourceCommandMatch.Groups[2].Value : string.Empty;
                        cmd.MessageText = forceSourceCommandMatch.Success ? forceSourceCommandMatch.Groups[1].Value : cmd.MessageText.Trim();
                        await _browserService.LaunchAsync(cmd.MessageText.Trim(), windowTitle);
                    }
                }

                //File.WriteAllLines(Program.QueueFile, queue.ToArray());
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
