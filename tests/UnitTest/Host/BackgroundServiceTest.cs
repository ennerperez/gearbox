using System;
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
    public class BackgroundServiceTest : System.IDisposable
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

        #region IDisposable

        private void ReleaseUnmanagedResources()
        {
            // TODO release unmanaged resources here
        }

        protected virtual void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();
            if (disposing)
            {
                _backgroundService.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~BackgroundServiceTest()
        {
            Dispose(false);
        }

        #endregion
    }
}
