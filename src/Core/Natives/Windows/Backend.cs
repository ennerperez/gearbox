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
        private readonly AssemblyMetadata _metadata = Assembly.GetEntryAssembly().ReadMetadata();

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
            var product = _metadata.Product;
            if (string.IsNullOrWhiteSpace(product))
            {
                return RegisterStatus.Unregistered;
            }

            var registeredCapabilityKey = _registerKey?.GetValue(product) as string;
            if (!IsSameRegistryPath(registeredCapabilityKey, CapabilityKey))
            {
                return RegisterStatus.Unregistered;
            }

            using var capabilityReg = Registry.CurrentUser.OpenSubKey(CapabilityKey);
            if (capabilityReg == null)
            {
                return RegisterStatus.Unregistered;
            }

            var openCommand = ReadOpenCommand();
            if (string.IsNullOrWhiteSpace(openCommand))
            {
                return RegisterStatus.Unregistered;
            }

            return IsCurrentOpenCommand(openCommand)
                ? RegisterStatus.Registered
                : RegisterStatus.Updated;
        }

        public Task<bool> RegisterAsync()
        {
            _logger.LogInformation("Registering...");

            var appReg = Registry.CurrentUser.CreateSubKey(AppKey);
            RegisterCapabilities(appReg);

            _registerKey?.SetValue(_metadata.Product, CapabilityKey);

            HandleUrls();
            OpenSettings();

            _logger.LogInformation("Please set {Product} as the default browser in Settings.", _metadata.Product);
            _notificationService.ShowAsync(new Notification("Register as default browser.", $"Please set {_metadata.Product} as the default browser in Settings."));
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

            handlerReg.SetValue(string.Empty, _metadata.Product ?? string.Empty);
            handlerReg.SetValue("FriendlyTypeName", _metadata.Product ?? string.Empty);
            handlerReg.CreateSubKey("shell\\open\\command").SetValue("", AppOpenUrlCommand);
        }

        public Task<bool> UnregisterAsync()
        {
            _logger.LogInformation("Unregistering...");

            RemoveUrlClass();
            RemoveCapabilities();
            RemoveRegisteredApplication();
            OpenSettings();

            return Task.FromResult(true);
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

        public void OpenSettings() => Process.Start(new ProcessStartInfo { FileName = $"ms-settings:defaultapps?registeredAppUser={_metadata.Product}", UseShellExecute = true });
        public void StartHost()
        {
            var background = Process.GetProcessesByName($"{_metadata.Product ?? "Gearbox"}.Host");
            if (background.Length != 0)
            {
                return;
            }

            _logger.LogWarning("Host is not running.");
            var hostPath = Path.Combine(AppContext.BaseDirectory, $"{_metadata.Product ?? "Gearbox"}.Host.exe");
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

        private string AppOpenUrlCommand => _metadata.Assembly?.Replace(".dll", ".exe", StringComparison.InvariantCultureIgnoreCase) + " %1";
        private string AppKey => $"SOFTWARE\\{_metadata.Product}";
        private string UrlKey => $"SOFTWARE\\Classes\\{_metadata.Product}URL";
        private string CapabilityKey => $"SOFTWARE\\{_metadata.Product}\\Capabilities";
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

            capabilityReg.SetValue("ApplicationName", _metadata.Product ?? string.Empty);
            capabilityReg.SetValue("ApplicationIcon", $"{_metadata.Assembly?.Replace(".dll", ".exe", StringComparison.InvariantCultureIgnoreCase)},0");
            capabilityReg.SetValue("ApplicationDescription", _metadata.Description ?? string.Empty);

            // Set up protocols we want to handle.
            var urlAssocReg = capabilityReg.CreateSubKey("URLAssociations");
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (urlAssocReg == null)
            {
                return;
            }

            urlAssocReg.SetValue("http", _metadata.Product + "URL");
            urlAssocReg.SetValue("https", _metadata.Product + "URL");
            urlAssocReg.SetValue("ftp", _metadata.Product + "URL");
            urlAssocReg.SetValue("ftps", _metadata.Product + "URL");
        }

        private string ReadOpenCommand()
        {
            using var commandReg = Registry.CurrentUser.OpenSubKey($"{UrlKey}\\shell\\open\\command");
            return commandReg?.GetValue(string.Empty) as string ?? string.Empty;
        }

        private void RemoveRegisteredApplication()
        {
            var product = _metadata.Product;
            if (string.IsNullOrWhiteSpace(product))
            {
                return;
            }

            var currentValue = _registerKey?.GetValue(product) as string;
            if (IsSameRegistryPath(currentValue, CapabilityKey))
            {
                _registerKey?.DeleteValue(product, false);
            }
        }

        private void RemoveCapabilities()
        {
            if (string.IsNullOrWhiteSpace(_metadata.Product))
            {
                return;
            }

            using (var appReg = Registry.CurrentUser.OpenSubKey(AppKey, true))
            {
                appReg?.DeleteSubKeyTree("Capabilities", false);
            }

            var removeAppKey = false;
            using (var remainingAppReg = Registry.CurrentUser.OpenSubKey(AppKey))
            {
                removeAppKey = remainingAppReg != null && remainingAppReg.GetValueNames().Length == 0 && remainingAppReg.GetSubKeyNames().Length == 0;
            }

            if (removeAppKey)
            {
                Registry.CurrentUser.DeleteSubKeyTree(AppKey, false);
            }
        }

        private void RemoveUrlClass()
        {
            if (!IsUrlClassSafeToRemove())
            {
                return;
            }

            Registry.CurrentUser.DeleteSubKeyTree(UrlKey, false);
        }

        private bool IsUrlClassSafeToRemove()
        {
            var product = _metadata.Product;
            if (string.IsNullOrWhiteSpace(product))
            {
                return false;
            }

            using var urlReg = Registry.CurrentUser.OpenSubKey(UrlKey);
            if (urlReg == null)
            {
                return false;
            }

            var name = urlReg.GetValue(string.Empty) as string;
            var friendlyName = urlReg.GetValue("FriendlyTypeName") as string;
            if (string.Equals(name, product, StringComparison.Ordinal) || string.Equals(friendlyName, product, StringComparison.Ordinal))
            {
                return true;
            }

            if (CapabilitiesPointToUrlClass())
            {
                return true;
            }

            var openCommand = ReadOpenCommand();
            return IsCurrentOpenCommand(openCommand);
        }

        private bool CapabilitiesPointToUrlClass()
        {
            using var urlAssocReg = Registry.CurrentUser.OpenSubKey($"{CapabilityKey}\\URLAssociations");
            if (urlAssocReg == null)
            {
                return false;
            }

            var urlClass = $"{_metadata.Product}URL";
            return string.Equals(urlAssocReg.GetValue("http") as string, urlClass, StringComparison.Ordinal)
                || string.Equals(urlAssocReg.GetValue("https") as string, urlClass, StringComparison.Ordinal);
        }

        private bool IsCurrentOpenCommand(string openCommand)
        {
            var currentPath = GetCommandExecutablePath(AppOpenUrlCommand);
            var registeredPath = GetCommandExecutablePath(openCommand);
            return !string.IsNullOrWhiteSpace(currentPath)
                && string.Equals(registeredPath, currentPath, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetCommandExecutablePath(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return string.Empty;
            }

            var trimmed = command.Trim();
            if (trimmed[0] == '"')
            {
                var closingQuote = trimmed.IndexOf('"', 1);
                return closingQuote > 1 ? trimmed.Substring(1, closingQuote - 1) : trimmed.Trim('"');
            }

            const string parameter = "%1";
            if (trimmed.EndsWith(parameter, StringComparison.Ordinal))
            {
                trimmed = trimmed.Substring(0, trimmed.Length - parameter.Length).Trim();
            }

            return trimmed.Trim('"');
        }

        private static bool IsSameRegistryPath(string left, string right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }
}
