using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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
    public class BackendTest
    {
        private readonly IBackend _backend;

        public BackendTest()
        {
            Assembly.GetEntryAssembly()?.ReadMetadata();

            var notificationService = Substitute.For<INotificationService>();
            var logger = Substitute.For<ILogger<Backend>>();
#pragma warning disable CA1416
            _backend = new Backend(notificationService, logger);
#pragma warning restore CA1416
        }

    }
}
