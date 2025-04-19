using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Gearbox.Core.Interfaces;
using Gearbox.Core.Services;
#if LINUX
using Gearbox.Core.Natives.Linux;
#elif MACOS
using Gearbox.Core.Natives.MacOS;
#else
using Gearbox.Core.Natives.Windows;
#endif
using Gearbox.Runner.Services;
using LiteDB.Async;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Gearbox.UnitTest.Runner
{
    [ExcludeFromCodeCoverage]
    [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
    public class BackgroundServiceTest
    {
        private readonly BackgroundService _backgroundService;
        private readonly QueueService _queueService;

        public BackgroundServiceTest()
        {
            Assembly.GetEntryAssembly()?.ReadMetadata();

            var backend = Substitute.For<IBackend>();
            var browserService = Substitute.For<IBrowserService>();
            var logger = Substitute.For<ILogger<BackgroundService>>();

            _queueService = new QueueService(new LiteDatabaseAsync($"{Guid.NewGuid()}.qdb") { UtcDate = true }, Substitute.For<ILogger<IQueueService>>());
            _backgroundService = new BackgroundService(backend, browserService, _queueService, logger);
        }

        [Theory]
        [InlineData("--register")]
        [InlineData("--unregister")]
        [InlineData("https://google.com")]
        public async Task ExecuteAsync(string command)
        {
            await _backgroundService.StartAsync(CancellationToken.None);
            await _queueService.SendMessageAsync(Metadata.Product ?? "Test", command);
            Thread.Sleep(3000);
            await _backgroundService.StopAsync(CancellationToken.None);
            Assert.True(true);
        }
    }
}
