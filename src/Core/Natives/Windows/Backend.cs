using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Gearbox.Core.Interfaces;
using Gearbox.Core.Natives.Windows.Interop;
using Gearbox.Core.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Notification = Gearbox.Core.Models.Notification;

namespace Gearbox.Core.Natives.Windows
{
    [SupportedOSPlatform("windows")]
    public class Backend : IBackend
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<IBackend> _logger;

        public Backend(INotificationService notificationService, ILogger<IBackend> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public string GetActiveWindowTitle()
        {
            var result = string.Empty;
            const int nChars = 256;
            var buff = new StringBuilder(nChars);
            var handle = User32.GetForegroundWindow();

            if (User32.GetWindowText(handle, buff.ToString(), nChars) > 0)
            {
                result = buff.ToString();
            }

            return result;
        }

        public RegisterStatus GetRegisterStatus()
        {
            throw new NotImplementedException();
        }

        public Task<bool> RegisterAsync()
        {
            _logger.LogInformation("Registering...");

            var appReg = Registry.CurrentUser.CreateSubKey(AppKey);
            RegisterCapabilities(appReg);

            _registerKey?.SetValue(AssemblyMetadata.Product, CapabilityKey);

            HandleUrls();
            OpenSettings();

            _logger.LogInformation("Please set {Product} as the default browser in Settings.", AssemblyMetadata.Product);
            _notificationService.ShowAsync(new Notification("Register as deafult browser.", $"Please set {AssemblyMetadata.Product} as the default browser in Settings."));
            return Task.FromResult(true);
        }

        private void HandleUrls()
        {
            var handlerReg = Registry.CurrentUser.CreateSubKey(UrlKey);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (handlerReg == null)
            {
                return;
            }

            handlerReg.SetValue(string.Empty, AssemblyMetadata.Product ?? string.Empty);
            handlerReg.SetValue("FriendlyTypeName", AssemblyMetadata.Product ?? string.Empty);
            handlerReg.CreateSubKey("shell\\open\\command").SetValue("", AppOpenUrlCommand);
        }

        public Task<bool> UnregisterAsync()
        {
            throw new NotImplementedException();
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
                    await _notificationService.ShowAsync(new Notification("Updated location", $"{AssemblyMetadata.Product} has been re-registered with a new path."));
                    return true;
            }
            return false;
        }

        public void OpenSettings() => Process.Start(new ProcessStartInfo { FileName = $"ms-settings:defaultapps?registeredAppUser={AssemblyMetadata.Product}", UseShellExecute = true });
        public void StartHost()
        {
            var background = Process.GetProcessesByName($"{AssemblyMetadata.Product ?? "Gearbox"}.Host");
            if (background.Length != 0)
            {
                return;
            }

            _logger.LogWarning("Host is not running.");
            var hostPath = Path.Combine(AppContext.BaseDirectory, $"{AssemblyMetadata.Product ?? "Gearbox"}.Host.exe");
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

        private string AppOpenUrlCommand => AssemblyMetadata.Assembly?.Replace(".dll", ".exe", StringComparison.InvariantCultureIgnoreCase) + " %1";
        private string AppKey => $"SOFTWARE\\{AssemblyMetadata.Product}";
        private string UrlKey => $"SOFTWARE\\Classes\\{AssemblyMetadata.Product}URL";
        private string CapabilityKey => $"SOFTWARE\\{AssemblyMetadata.Product}\\Capabilities";

        private readonly RegistryKey _registerKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\RegisteredApplications", true);

        // private RegistryKey? AppRegKey => Registry.CurrentUser.OpenSubKey(AppKey);
        // private RegistryKey? UrlRegKey => Registry.CurrentUser.OpenSubKey(UrlKey);
        private void RegisterCapabilities(RegistryKey appReg)
        {
            // Register capabilities.
            var capabilityReg = appReg.CreateSubKey("Capabilities");
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (capabilityReg == null)
            {
                return;
            }

            capabilityReg.SetValue("ApplicationName", AssemblyMetadata.Product ?? string.Empty);
            capabilityReg.SetValue("ApplicationIcon", $"{AssemblyMetadata.Assembly?.Replace(".dll", ".exe", StringComparison.InvariantCultureIgnoreCase)},0");
            capabilityReg.SetValue("ApplicationDescription", AssemblyMetadata.Description ?? string.Empty);

            // Set up protocols we want to handle.
            var urlAssocReg = capabilityReg.CreateSubKey("URLAssociations");
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (urlAssocReg == null)
            {
                return;
            }

            urlAssocReg.SetValue("http", AssemblyMetadata.Product + "URL");
            urlAssocReg.SetValue("https", AssemblyMetadata.Product + "URL");
            urlAssocReg.SetValue("ftp", AssemblyMetadata.Product + "URL");
            urlAssocReg.SetValue("ftps", AssemblyMetadata.Product + "URL");
        }
    }
}
