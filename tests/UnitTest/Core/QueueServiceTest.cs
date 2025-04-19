using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Gearbox.Core.Interfaces;
using Gearbox.Core.Services;
using LiteDB.Async;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Gearbox.UnitTest.Core
{
    [ExcludeFromCodeCoverage]
    [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
    public class QueueServiceTest
    {
        private readonly ILogger<IQueueService> _logger;

        public QueueServiceTest()
        {
            Assembly.GetEntryAssembly()?.ReadMetadata();
            _logger = Substitute.For<ILogger<IQueueService>>();
        }

        [Theory]
        [InlineData("Test")]
        public async Task QueueAsync(string queueName)
        {
            var queueService = new QueueService(new LiteDatabaseAsync($"{Guid.NewGuid()}.qdb") { UtcDate = true }, _logger);
            var content = Guid.NewGuid().ToString();
            var receipt = await queueService.SendMessageAsync(queueName, content);
            Assert.NotNull(receipt);
        }

        [Theory]
        [InlineData("Test")]
        public async Task DequeueAsync(string queueName)
        {
            var queueService = new QueueService(new LiteDatabaseAsync($"{Guid.NewGuid()}.qdb") { UtcDate = true }, _logger);
            var content = Guid.NewGuid().ToString();
            await queueService.SendMessageAsync(queueName, content);
            var message = await queueService.ReceiveMessageAsync(queueName);
            var messages = await queueService.PeekMessagesAsync(queueName);
            Assert.NotNull(message?.MessageText);
            Assert.True(messages != null && messages.Count() == 0);
        }
    }
}
