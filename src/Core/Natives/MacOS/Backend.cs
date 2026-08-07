using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Gearbox.Core.Interfaces;
using Gearbox.Core.Natives.MacOS.Interop;
using Gearbox.Core.Types;
using Microsoft.Extensions.Logging;
using Notification = Gearbox.Core.Models.Notification;

namespace Gearbox.Core.Natives.MacOS
{
    // ReSharper disable once InconsistentNaming
    [SupportedOSPlatform("macOS")]
    public class Backend : IBackend
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<Backend> _logger;
        private readonly AssemblyMetadata _metadata = Assembly.GetEntryAssembly().ReadMetadata();

        public Backend(INotificationService notificationService, ILogger<Backend> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public string GetActiveWindowTitle()
        {
            return Xdo.GetActiveWindowName();
        }

        public RegisterStatus GetRegisterStatus()
        {
            var handlers = ReadDefaultUrlHandlers();
            return handlers.TryGetValue("http", out var httpHandler) &&
                   handlers.TryGetValue("https", out var httpsHandler) &&
                   IsCurrentAppHandler(httpHandler) &&
                   IsCurrentAppHandler(httpsHandler)
                ? RegisterStatus.Registered
                : RegisterStatus.Unregistered;
        }

        public Task<bool> RegisterAsync()
        {
            _logger.LogInformation("Registering...");

            OpenSettings();

            _logger.LogInformation("Please set {Product} as the default browser in Settings.", _metadata.Product);
            _notificationService.ShowAsync(new Notification("Register as default browser.", $"Please set {_metadata.Product} as the default browser in Settings."));
            return Task.FromResult(true);
        }

        public async Task<bool> UnregisterAsync()
        {
            _logger.LogInformation("Unregistering...");

            OpenSettings();

            _logger.LogInformation("Please choose a different default browser in Settings.");
            await _notificationService.ShowAsync(new Notification("Change default browser.", "Please choose a different default browser in Settings."));
            return true;
        }

        public async Task<bool> RegisterOrUnregisterAsync()
        {
            var status = GetRegisterStatus();

            switch (status)
            {
                case RegisterStatus.Unregistered:
                    await RegisterAsync();
                    return true;
                case RegisterStatus.Registered:
                    await UnregisterAsync();
                    return true;
                case RegisterStatus.Updated:
                    await UnregisterAsync(); // Unregister the old path
                    await RegisterAsync(); // Register with the new path
                    await _notificationService.ShowAsync(new Notification("Updated location", $"{_metadata.Product} has been re-registered with a new path."));
                    return true;
            }
            return false;
        }

        public void OpenSettings() => Process.Start(new ProcessStartInfo { FileName = "x-apple.systempreferences:com.apple.Desktop-Settings.extension", UseShellExecute = true });

        private bool IsCurrentAppHandler(string handler)
        {
            var product = _metadata.Product ?? "Gearbox";
            var appName = Path.GetFileNameWithoutExtension(_metadata.Assembly) ?? product;
            return MatchesIdentifier(handler, product) || MatchesIdentifier(handler, appName);
        }

        private static bool MatchesIdentifier(string handler, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var identifierPart = new string(value.Where(char.IsLetterOrDigit).ToArray());
            if (string.IsNullOrWhiteSpace(identifierPart))
            {
                return false;
            }

            return string.Equals(handler, value, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(handler, identifierPart, StringComparison.OrdinalIgnoreCase) ||
                   handler.EndsWith($".{identifierPart}", StringComparison.OrdinalIgnoreCase);
        }

        private static Dictionary<string, string> ReadDefaultUrlHandlers()
        {
            var output = RunProcess("/usr/bin/defaults", "read", "com.apple.LaunchServices/com.apple.launchservices.secure", "LSHandlers");
            var handlers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string scheme = null;
            string handler = null;
            foreach (var rawLine in output.Split(Environment.NewLine))
            {
                var line = rawLine.Trim();
                if (line == "{")
                {
                    scheme = null;
                    handler = null;
                    continue;
                }

                if (line.StartsWith("LSHandlerURLScheme = ", StringComparison.Ordinal))
                {
                    scheme = TrimDefaultsValue(line["LSHandlerURLScheme = ".Length..]);
                    continue;
                }

                if (line.StartsWith("LSHandlerRoleAll = ", StringComparison.Ordinal))
                {
                    handler = TrimDefaultsValue(line["LSHandlerRoleAll = ".Length..]);
                    continue;
                }

                if (line.StartsWith("LSHandlerRoleViewer = ", StringComparison.Ordinal))
                {
                    handler ??= TrimDefaultsValue(line["LSHandlerRoleViewer = ".Length..]);
                    continue;
                }

                if ((line == "}," || line == "}") &&
                    !string.IsNullOrWhiteSpace(scheme) &&
                    !string.IsNullOrWhiteSpace(handler))
                {
                    handlers[scheme] = handler;
                }
            }

            return handlers;
        }

        private static string RunProcess(string fileName, params string[] arguments)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                }
            };

            foreach (var argument in arguments)
            {
                process.StartInfo.ArgumentList.Add(argument);
            }

            if (!process.Start())
            {
                return string.Empty;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode == 0 ? output : string.Empty;
        }

        private static string TrimDefaultsValue(string value)
        {
            return value.Trim().TrimEnd(';').Trim().Trim('"');
        }

        public void StartHost()
        {
            var background = Process.GetProcessesByName($"{_metadata.Product ?? "Gearbox"}.Host");
            if (background.Length != 0)
            {
                return;
            }

            _logger.LogWarning("Host is not running.");
            var hostPath = Path.Combine(AppContext.BaseDirectory, $"{_metadata.Product ?? "Gearbox"}.Host");
            if (File.Exists(hostPath))
            {
                _logger.LogInformation("Starting host at {HostPath}", hostPath);
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = hostPath,
                        UseShellExecute = true,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false,
                        CreateNoWindow = true,
                    }
                };
                process.Start();
            }
            else
            {
                _logger.LogError("Host executable not found at {HostPath}", hostPath);
            }
        }
    }
}
