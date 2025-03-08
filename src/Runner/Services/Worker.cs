using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Gearbox.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gearbox.Runner.Services
{
  public partial class Worker : BackgroundService
  {
    private readonly IBackend _backend;
    private readonly IBrowserService _browserService;
    private readonly ILogger<Worker> _logger;
    public Worker(IBackend backend, IBrowserService browserService, ILogger<Worker> logger)
    {
      _backend = backend;
      _browserService = browserService;
      _logger = logger;
    }

    private static readonly string[] s_registerCommand = ["--register", "-r"];
    private static readonly string[] s_unregisterCommand = ["--unregister", "-u"];
    private static readonly string[] s_forceSourceCommand = ["--forceSource", "-fS"];

    [GeneratedRegex("\\-r|\\-\\-register", RegexOptions.Compiled)] private static partial Regex RegisterCommandRegex();
    [GeneratedRegex("\\-u|\\-\\-unregister", RegexOptions.Compiled)] private static partial Regex UnregisterCommandRegex();
    [GeneratedRegex("(.*) [\\-fS|\\-\\-forceSource]{1,} (.*)", RegexOptions.Compiled)]
    private static partial Regex ForceSourceCommandRegex();

    public static readonly string[] Commands = s_registerCommand.Concat(s_unregisterCommand).ToArray();

    private static readonly CancellationTokenSource s_cancellationTokenSource = new CancellationTokenSource();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested && !s_cancellationTokenSource.IsCancellationRequested)
      {
        var queue = new Queue<string>();
        if (File.Exists(Program.QueueFile))
        {
          var lines = File.ReadLines(Program.QueueFile);
          queue = new Queue<string>(lines);
        }
        if (queue.Count <= 0) { return; }

        var cmd = queue.Dequeue();
        if (!string.IsNullOrWhiteSpace(cmd))
        {

          var forceRegisterCommandMatch = RegisterCommandRegex().Match(cmd);
          var forceUnregisterSourceCommandMatch = UnregisterCommandRegex().Match(cmd);

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
            var forceSourceCommandMatch = ForceSourceCommandRegex().Match(cmd);
            var windowTitle = forceSourceCommandMatch.Success ? forceSourceCommandMatch.Groups[2].Value : string.Empty;
            cmd = forceSourceCommandMatch.Success ? forceSourceCommandMatch.Groups[1].Value : cmd.Trim();
            await _browserService.LaunchAsync(cmd.Trim(), windowTitle);
          }
        }
        File.WriteAllLines(Program.QueueFile, queue.ToArray());
        await Task.Delay(1000, stoppingToken);
      }
    }

  }
}
