using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Gearbox.Core.Interfaces;
using Gearbox.Core.Services;
using Gearbox.Host.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Gearbox.UnitTest.Host
{
    [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
    public class BackgroundServiceTest
    {
        private readonly BackgroundService _backgroundService;
        private readonly MemoryQueueService _queueService;

        public BackgroundServiceTest()
        {
            Assembly.GetEntryAssembly()?.ReadMetadata();

            var backend = Substitute.For<IBackend>();
            var browserService = Substitute.For<IBrowserService>();
            var logger = Substitute.For<ILogger<BackgroundService>>();

            _queueService = new MemoryQueueService();
            _backgroundService = new BackgroundService(backend, browserService, _queueService, logger);
        }

    }
}
