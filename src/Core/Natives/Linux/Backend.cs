using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Gearbox.Core.Interfaces;
using Gearbox.Core.Models;
using Gearbox.Core.Natives.Linux.Interop;
using Gearbox.Core.Types;
using Microsoft.Extensions.Logging;

namespace Gearbox.Core.Natives.Linux
{
    [SupportedOSPlatform("linux")]
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
            var name = Xdo.GetActiveWindowName();
            return name;
        }

        public RegisterStatus GetRegisterStatus()
        {
            var currentValue = Xdg.GetSetting(Xdg.DEFAULT_WEB_BROWSER);
            return currentValue == $"{Metadata.Product}.desktop" ? RegisterStatus.Registered : RegisterStatus.Unregistered;
        }

        public Task<bool> RegisterAsync()
        {
            _logger.LogInformation("Registering...");

            HandleUrls($"{Metadata.Product}.desktop");
            OpenSettings();

            _logger.LogInformation($"Please set {Metadata.Product} as the default browser in Settings.");
            _notificationService.Show(new Notification("Registered as a browser.", $"Please set {Metadata.Product} as the default browser in Settings."));
            return Task.FromResult(true);
        }

        private void HandleUrls(string appName)
        {
            var mimes = "application/pdf;application/rdf+xml;application/rss+xml;application/xhtml+xml;application/xhtml_xml;application/xml;image/gif;image/jpeg;image/png;image/webp;text/html;text/xml;x-scheme-handler/http;x-scheme-handler/https;x-scheme-handler/ipfs;x-scheme-handler/ipns;application/x-opera-download;"
                .Split(";").Where(m => !string.IsNullOrWhiteSpace(m)).ToArray();
            foreach (var mime in mimes)
            {
                try
                {
                    Xdg.Mime(appName, mime);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Failed to set URL {Url}", mime);
                }
            }
        }

        public Task<bool> UnregisterAsync()
        {
            var currentValue = Xdg.GetSetting(Xdg.DEFAULT_WEB_BROWSER);
            HandleUrls(currentValue);
            Xdg.SetSetting(Xdg.DEFAULT_WEB_BROWSER, currentValue);
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
                    _notificationService.Show(new Notification("Updated location", $"{Metadata.Product} has been re-registered with a new path."));
                    return true;
            }
            return false;
        }

        public void OpenSettings()
        {
            //TODO: Open Default App Settings
            switch (Xdg.CurrentDesktop())
            {
                case Xdg.GNOME_DESKTOP:
                    Process.Start("gnome-control-center", "applications")
                        .WaitForExit(new TimeSpan(0, 0, 30));
                    break;
                case Xdg.KDE_DESKTOP:
                    var sv = Environment.GetEnvironmentVariable("KDE_SESSION_VERSION") ?? "6";
                    Process.Start($"kcmshell{sv}", "kcm_componentchooser")
                        .WaitForExit(new TimeSpan(0, 0, 30));
                    break;
            }
        }
    }
}
