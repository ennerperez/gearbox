using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Gearbox.Core.Interfaces;
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

        public Backend(INotificationService notificationService, ILogger<Backend> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public string GetActiveWindowTitle()
        {
#if OSX
            var windowInfo = QuartzCore.CGWindowListCopyWindowInfo(CGWindowListOption.OnScreenOnly, 0);
            var values = (NSArray)Runtime.GetNSObject<NSArray>(windowInfo);

            var windowList = new List<QuartzCore.kCGWindow>();
            for (ulong i = 0, len = values.Count; i < len; i++)
            {
                var window = Runtime.GetNSObject(values.ValueAt(i));
                var item = new QuartzCore.kCGWindow();
                item.Read(window);
                windowList.Add(item);
            }
#endif
            throw new System.NotImplementedException();
        }

        public RegisterStatus GetRegisterStatus()
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> RegisterAsync()
        {
            _logger.LogInformation("Registering...");

            OpenSettings();

            _logger.LogInformation($"Please set {Metadata.Product} as the default browser in Settings.");
            _notificationService.Show(new Notification("Registered as a browser.", $"Please set {Metadata.Product} as the default browser in Settings."));
            return Task.FromResult(true);
        }

        public Task<bool> UnregisterAsync()
        {
            throw new System.NotImplementedException();
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
                    _notificationService.Show(new Notification("Updated location", $"{Metadata.Product} has been re-registered with a new path."));
                    return true;
            }
            return false;
        }

        public void OpenSettings() => Process.Start(new ProcessStartInfo { FileName = "x-apple.systempreferences:com.apple.Desktop-Settings.extension", UseShellExecute = true });
        public void StartHost()
        {
            var background = Process.GetProcessesByName($"{Metadata.Product ?? "Gearbox"}.Host");
            if (background.Length != 0)
            {
                return;
            }

            _logger.LogWarning("Host is not running.");
            var hostPath = Path.Combine(AppContext.BaseDirectory, $"{Metadata.Product ?? "Gearbox"}.Host");
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
