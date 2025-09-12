using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Gearbox.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Gearbox.Host.Services
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

        [GeneratedRegex("\\-r|\\-\\-register", RegexOptions.Compiled)]
        private static partial Regex RegisterCommandRegex();

        [GeneratedRegex("\\-u|\\-\\-unregister", RegexOptions.Compiled)]
        private static partial Regex UnregisterCommandRegex();

        [GeneratedRegex("(.*) [\\-fS|\\-\\-forceSource]{1,} (.*)", RegexOptions.Compiled)]
        private static partial Regex ForceSourceCommandRegex();

        public static readonly IEnumerable<string> Commands = s_registerCommand.Concat(s_unregisterCommand).ToArray();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var cmd = await _queueService.ReceiveMessageAsync(AssemblyMetadata.Product ?? "Gearbox", cancellationToken: stoppingToken);
                if (cmd != null)
                {
                    var forceRegisterCommandMatch = RegisterCommandRegex().Match(cmd.MessageText);
                    var forceUnregisterSourceCommandMatch = UnregisterCommandRegex().Match(cmd.MessageText);

                    if (forceRegisterCommandMatch.Success)
                    {
                        _logger.LogInformation("Executing register command: {Command}", cmd.MessageText);
                        await _backend.RegisterAsync();
                    }
                    else if (forceUnregisterSourceCommandMatch.Success)
                    {
                        _logger.LogInformation("Executing unregister command: {Command}", cmd.MessageText);
                        await _backend.UnregisterAsync();
                    }
                    else
                    {
                        var windowTitle = string.Empty;
                        cmd.MessageText = cmd.MessageText.Trim();
                        var forceSourceCommandMatch = ForceSourceCommandRegex().Match(cmd.MessageText);
                        if (forceSourceCommandMatch.Success)
                        {
                            windowTitle = forceSourceCommandMatch.Groups[2].Value;
                            cmd.MessageText = forceSourceCommandMatch.Groups[1].Value;
                        }

                        _logger.LogInformation("Launching browser with command: {Command} and window title: {WindowTitle}", cmd.MessageText, windowTitle);
                        await _browserService.LaunchAsync(new Uri(cmd.MessageText.Trim()), windowTitle);
                    }
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
