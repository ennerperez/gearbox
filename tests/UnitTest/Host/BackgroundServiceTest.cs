using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Gearbox.Core.Interfaces;
using Gearbox.Core.Models;
using Gearbox.Core.Services;
using Gearbox.Host.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Gearbox.UnitTest.Host
{
    [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
    [SuppressMessage("Reliability", "CA2025:Ensure tasks using 'IDisposable' instances complete before the instances are disposed", Justification = "Tests await the service loop cancellation before the cancellation source is disposed.")]
    public class BackgroundServiceTest : System.IDisposable
    {
        private readonly IBackend _backend;
        private readonly IBrowserService _browserService;
        private readonly TestBackgroundService _backgroundService;
        private readonly IQueueService _queueService;

        public BackgroundServiceTest()
        {
            Assembly.GetEntryAssembly()?.ReadMetadata();

            _backend = Substitute.For<IBackend>();
            _browserService = Substitute.For<IBrowserService>();
            var logger = Substitute.For<ILogger<BackgroundService>>();

            _queueService = Substitute.For<IQueueService>();
            _backgroundService = new TestBackgroundService(_backend, _browserService, _queueService, logger);
        }

        [Theory]
        [InlineData("--register")]
        [InlineData("-r")]
        public async Task ExecuteAsyncRoutesRegisterCommand(string command)
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await ArrangeQueuedCommandAsync(command);

            _backend.RegisterAsync().Returns(_ =>
            {
                return CancelAndReturnTrueAsync(cancellationTokenSource);
            });

            await ExecuteUntilCancelledAsync(cancellationTokenSource.Token);

            await _backend.Received(1).RegisterAsync();
            await _backend.DidNotReceive().UnregisterAsync();
            await _browserService.DidNotReceive().LaunchAsync(Arg.Any<Uri>(), Arg.Any<string>());
        }

        [Theory]
        [InlineData("--unregister")]
        [InlineData("-u")]
        public async Task ExecuteAsyncRoutesUnregisterCommand(string command)
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await ArrangeQueuedCommandAsync(command);

            _backend.UnregisterAsync().Returns(_ =>
            {
                return CancelAndReturnTrueAsync(cancellationTokenSource);
            });

            await ExecuteUntilCancelledAsync(cancellationTokenSource.Token);

            await _backend.DidNotReceive().RegisterAsync();
            await _backend.Received(1).UnregisterAsync();
            await _browserService.DidNotReceive().LaunchAsync(Arg.Any<Uri>(), Arg.Any<string>());
        }

        [Fact]
        public async Task ExecuteAsyncRoutesUrlAndForceSourceToBrowserService()
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await ArrangeQueuedCommandAsync("https://example.test --forceSource Work");

            _browserService.LaunchAsync(Arg.Any<Uri>(), Arg.Any<string>()).Returns(_ =>
            {
                return CancelAndReturnTrueAsync(cancellationTokenSource);
            });

            await ExecuteUntilCancelledAsync(cancellationTokenSource.Token);

            await _backend.DidNotReceive().RegisterAsync();
            await _backend.DidNotReceive().UnregisterAsync();
            await _browserService.Received(1).LaunchAsync(
                Arg.Is<Uri>(uri => uri.AbsoluteUri == "https://example.test/"),
                "Work");
        }

        private async Task ArrangeQueuedCommandAsync(string command)
        {
            var message = await CreateQueueMessageAsync(command);
            _queueService.ReceiveMessageAsync(Arg.Any<string>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(message));
        }

        private async Task ExecuteUntilCancelledAsync(CancellationToken cancellationToken)
        {
            var exception = await Record.ExceptionAsync(() => _backgroundService.ExecuteForTestAsync(cancellationToken));

            exception.ShouldBeAssignableTo<OperationCanceledException>();
        }

        private static async Task<QueueMessage> CreateQueueMessageAsync(string messageText)
        {
            var queueService = new MemoryQueueService();
            var queueName = Guid.NewGuid().ToString();

            await queueService.SendMessageAsync(queueName, messageText);

            return await queueService.ReceiveMessageAsync(queueName);
        }

        private static async Task<bool> CancelAndReturnTrueAsync(CancellationTokenSource cancellationTokenSource)
        {
            await cancellationTokenSource.CancelAsync();
            return true;
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

        private sealed class TestBackgroundService : BackgroundService
        {
            public TestBackgroundService(IBackend backend, IBrowserService browserService, IQueueService queueService, ILogger<BackgroundService> logger)
                : base(backend, browserService, queueService, logger)
            {
            }

            public Task ExecuteForTestAsync(CancellationToken stoppingToken)
            {
                return ExecuteAsync(stoppingToken);
            }
        }
    }
}
