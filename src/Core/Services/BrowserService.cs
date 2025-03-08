using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Gearbox.Core.Interfaces;
using Gearbox.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Gearbox.Core.Services
{
  public partial class BrowserService : IBrowserService
  {
    private readonly IBackend _backend;
    private readonly INotificationService _notificationService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BrowserService> _logger;
    public BrowserService(INotificationService notificationService, IConfiguration configuration, IBackend backend, ILogger<BrowserService> logger)
    {
      _notificationService = notificationService;
      _configuration = configuration;
      _backend = backend;
      _logger = logger;
    }

    public Task<int> LaunchAsync(string url, string windowTitle = "")
    {
      try
      {
        if (string.IsNullOrEmpty(windowTitle))
        {
          windowTitle = _backend.GetActiveWindowTitle();
        }
        IBrowser? browser = null;

        var browsers = new Dictionary<string, Browser>();
        _configuration.Bind("browsers", browsers);

        _logger.LogInformation($"Attempting to launch \"{url}\"");

        var urlPreferences = new Dictionary<string, string>();
        _configuration.Bind("urls", urlPreferences);

        var sourcePreferences = new Dictionary<string, string>();
        _configuration.Bind("sources", sourcePreferences);

        var typesPreferences = new Dictionary<string, string>();
        _configuration.Bind("types", typesPreferences);

        var urlMatchKey = urlPreferences.FirstOrDefault(m => new Regex(m.Key.Replace("*", ".*")).Match(url).Success).Key;
        var sourceMatchKey = sourcePreferences.FirstOrDefault(m => new Regex(m.Key.Replace("*", ".*")).Match(windowTitle).Success).Key;
        var typeMatchKey = typesPreferences.FirstOrDefault(m => url.EndsWith(m.Key.Replace("*.", "."))).Key;

        var value = _configuration["general:default"] ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(urlMatchKey))
        {
          value = urlPreferences[urlMatchKey];
        }
        else if (!string.IsNullOrWhiteSpace(sourceMatchKey))
        {
          value = sourcePreferences[sourceMatchKey];
        }
        else if (!string.IsNullOrWhiteSpace(typeMatchKey))
        {
          value = typesPreferences[typeMatchKey];
        }

        /* LAUNCH */

        try
        {

          if (string.IsNullOrWhiteSpace(value)) throw new ApplicationException("Browser cannot be launched without a value.");

          browser = browsers[value];

          if (string.IsNullOrWhiteSpace(browser.Path)) throw new ApplicationException("Browser path cannot be launched without a value.");
          var process = PrepareNativeProcess(browser, url);
          process.Start();

          _notificationService.Show(new Notification("Browser launched.", $"{browser.Name} was launched."));
          return Task.FromResult(process.Id);
        }
        catch (Exception e)
        {
          _logger.LogError(e, $"Failed to launch \"{url}\" for \"{browser?.Name}\", {{Message}}", e.Message);
        }

      }
      catch (Exception e)
      {
        _logger.LogError(e, "Failed to launch browser, {Message}", e.Message);
        return Task.FromResult(0);
      }
      return Task.FromResult(0);
    }
    private static Process PrepareNativeProcess(IBrowser browser, string url)
    {
      var process = new Process();
      process.StartInfo = new ProcessStartInfo();
      if (!string.IsNullOrWhiteSpace(browser.Path))
      {
        if (OperatingSystem.IsLinux())
        {
          var flatpak = FlatpakRegex();
          var match = flatpak.Match(browser.Path);
          if (match.Success)
          {
            process.StartInfo.FileName = "flatpak";
            process.StartInfo.ArgumentList.Add("run");
            process.StartInfo.ArgumentList.Add(match.Groups[1].Value);
            process.StartInfo.WorkingDirectory = "/usr/bin";
          }
        }
        else
        {
          process.StartInfo.FileName = Environment.ExpandEnvironmentVariables(browser.Path);
          process.StartInfo.WorkingDirectory = Path.GetDirectoryName(process.StartInfo.FileName);
        }
      }
      if (!string.IsNullOrWhiteSpace(browser.Args))
      {
        process.StartInfo.ArgumentList.Add(browser.Args);
      }
      process.StartInfo.ArgumentList.Add($"{url}");
      return process;
    }

    [GeneratedRegex("flatpak run (.*)", RegexOptions.Compiled)]
    private static partial Regex FlatpakRegex();
  }
}
