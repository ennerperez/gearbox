using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Gearbox.Core.Interfaces;
#if LINUX
using Gearbox.Core.Natives.Linux;
#elif MACOS
using Gearbox.Core.Natives.MacOS;
#else
using Gearbox.Core.Natives.Windows;
#endif
using Gearbox.Runner.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Gearbox.UnitTest.Core
{
    [ExcludeFromCodeCoverage]
    [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
    public class BackendTest
    {
        private readonly IBackend _backend;

        public BackendTest()
        {
            Assembly.GetEntryAssembly()?.ReadMetadata();

            var notificationService = Substitute.For<INotificationService>();
            var logger = Substitute.For<ILogger<IBackend>>();

            _backend = new Backend(notificationService, logger);
        }

        [Fact]
        public async Task RegisterAsync()
        {
            var result = await _backend.RegisterAsync();
            Assert.True(result);
        }

        [Fact]
        public async Task UnregisterAsync()
        {
            var result = await _backend.UnregisterAsync();
            Assert.True(result);
        }

        [Fact]
        public async Task RegisterOrUnregisterAsync()
        {
            var result = await _backend.RegisterOrUnregisterAsync();
            Assert.True(result);
        }
    }
}
