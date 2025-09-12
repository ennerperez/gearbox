using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cysharp.Diagnostics;
using Gearbox.Core.Exceptions;
using Gearbox.Core.Interfaces;
using Gearbox.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Gearbox.Core.Services
{
    public class BrowserService : IBrowserService
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

        public async Task<bool> LaunchAsync(string url, string windowTitle = "")
        {
            try
            {
                if (string.IsNullOrEmpty(windowTitle))
                {
                    windowTitle = _backend.GetActiveWindowTitle();
                }

                var browsers = new Dictionary<string, Browser>();
                _configuration.Bind("browsers", browsers);

                var urlPreferences = new Dictionary<string, string>();
                _configuration.Bind("urls", urlPreferences);

                var sourcePreferences = new Dictionary<string, string>();
                _configuration.Bind("sources", sourcePreferences);

                var typesPreferences = new Dictionary<string, string>();
                _configuration.Bind("types", typesPreferences);

                var urlMatchKey = urlPreferences.FirstOrDefault(m => new Regex(m.Key.Replace("*", ".*", StringComparison.InvariantCultureIgnoreCase)).Match(url).Success).Key;
                var sourceMatchKey = sourcePreferences.FirstOrDefault(m => new Regex(m.Key.Replace("*", ".*", StringComparison.InvariantCultureIgnoreCase)).Match(windowTitle).Success).Key;
                var typeMatchKey = typesPreferences.FirstOrDefault(m => url.EndsWith(m.Key.Replace("*.", ".", StringComparison.InvariantCultureIgnoreCase), StringComparison.InvariantCultureIgnoreCase)).Key;

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

                Browser browser = null;
                try
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        throw new BrowserException("Browser cannot be launched without a value.");
                    }

                    browser = browsers[value];

                    if (string.IsNullOrWhiteSpace(browser.Path))
                    {
                        throw new BrowserException("Browser path cannot be launched without a value.", browser);
                    }

                    if (!browser.IsInstalled)
                    {
                        throw new BrowserException("Browser is not installed.", browser);
                    }

                    _logger.LogInformation("Attempting to open \"{Url}\" with \"{Browser}\"", url, browser.Name);

                    await _notificationService.ShowAsync(new Notification("Browser launched.", $"{browser.Name} was launched."));

                    await StartProcess(browser, url);

                    return true;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to open \"{Url}\" for \"{Browser}\", {Message}", url, browser?.Name, e.Message);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to launch browser, {Message}", e.Message);
            }

            return false;
        }

        public async Task<bool> LaunchAsync(Uri url, string windowTitle = "")
        {
            return await LaunchAsync(url.ToString(), windowTitle);
        }

        public async Task<bool> LaunchAsync(IBrowser browser, string url)
        {
            try
            {
                /* LAUNCH */

                try
                {
                    if (string.IsNullOrWhiteSpace(browser.Path))
                    {
                        throw new BrowserException("Browser path cannot be launched without a value.", browser);
                    }

                    if (!browser.IsInstalled)
                    {
                        throw new BrowserException("Browser is not installed.", browser);
                    }

                    _logger.LogInformation("Attempting to open \"{Url}\" with \"{Browser}\"", url, browser.Name);

                    await _notificationService.ShowAsync(new Notification("Browser launched.", $"{browser.Name} was launched."));

                    await StartProcess(browser, url);

                    return true;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to open \"{Url}\" for \"{Browser}\", {Message}", url, browser.Name, e.Message);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to launch browser, {Message}", e.Message);
            }

            return false;
        }

        public async Task<bool> LaunchAsync(IBrowser browser, Uri url)
        {
            return await LaunchAsync(browser, url.ToString());
        }

        private async Task StartProcess(IBrowser browser, string url)
        {
            if (string.IsNullOrWhiteSpace(browser.Path)) { throw new OperationCanceledException("Browser path cannot be launched without a value."); }
            ProcessX.AcceptableExitCodes = new[] { 0, 24 };
            try
            {
                var fileName = Path.GetFileName(Environment.ExpandEnvironmentVariables(browser.Path));
                if (OperatingSystem.IsWindows())
                {
                    fileName = Environment.ExpandEnvironmentVariables(browser.Path);
                }
                var startInfo = new ProcessStartInfo()
                {
                    FileName = fileName,
                    Arguments = string.Join(" ", new[] { browser.Command, browser.Args, url }),
                    WorkingDirectory = browser.WorkingDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                };
                await ProcessX.StartAsync(startInfo).WaitAsync();
            }
            catch (ProcessErrorException ex)
            {
                if (!ProcessX.AcceptableExitCodes.Contains(ex.ExitCode))
                {
                    _logger.LogDebug("ERROR, ExitCode: {ExitCode}", ex.ExitCode);
                    throw;
                }
            }
        }

        public IDictionary<string, T> GetBrowsers<T>() where T : IBrowser
        {
            var browsers = new Dictionary<string, T>();
            _configuration.Bind("browsers", browsers);
            return browsers;
        }
    }
}
