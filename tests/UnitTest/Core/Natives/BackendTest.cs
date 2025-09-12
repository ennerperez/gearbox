using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Gearbox.Core.Interfaces;
#if LINUX
using Gearbox.Core.Natives.Linux;
#elif OSX
using Gearbox.Core.Natives.MacOS;
#elif WINDOWS
using Gearbox.Core.Natives.Windows;
#endif
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Gearbox.UnitTest.Core.Natives
{
    [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    public class BackendTest
    {
        private readonly IBackend _backend;

        public BackendTest()
        {
            Assembly.GetEntryAssembly()?.ReadMetadata();
            AssemblyMetadata.Product = "Gearbox";

            var notificationService = Substitute.For<INotificationService>();
            var logger = Substitute.For<ILogger<Backend>>();
            _backend = new Backend(notificationService, logger);
        }

        [Fact]
        private async Task Should_Open_Settings()
        {
            var t1 = new Thread(() =>
            {
                _backend.OpenSettings();
            });
            t1.Start();
            await Task.Delay(1000);
            var processName = string.Empty;
#if LINUX
            switch (Xdg.CurrentDesktop())
            {
                case Xdg.GNOME_DESKTOP:
                    processName = "gnome-control-center";
                    break;
                case Xdg.KDE_DESKTOP:
                    var sv = Environment.GetEnvironmentVariable("KDE_SESSION_VERSION") ?? "6";
                    processName = $"kcmshell{sv}";
                    break;
            }
#elif OSX
            processName = "x-apple.systempreferences";
#elif WINDOWS
            processName = "ms-settings:";
#endif
            var process = Process.GetProcessesByName(processName);
            process.Length.ShouldBeGreaterThan(0);
        }

        [Fact]
        private async Task Should_Start_Host()
        {
            var t1 = new Thread(() =>
            {
                _backend.StartHost();
            });
            t1.Start();
            await Task.Delay(3000);
            var processName = $"{AssemblyMetadata.Product ?? "Gearbox"}.Host";
            var process = Process.GetProcessesByName(processName);
            process.Length.ShouldBeGreaterThan(0);
        }
    }
}

