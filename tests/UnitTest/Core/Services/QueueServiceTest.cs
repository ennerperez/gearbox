using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Gearbox.Core.Models;
using Gearbox.Core.Services;
using Shouldly;
using Xunit;

namespace Gearbox.UnitTest.Core.Services
{
    [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
    public class QueueServiceTest
    {
        private readonly MemoryQueueService _queueService;

        public QueueServiceTest()
        {
            Assembly.GetEntryAssembly()?.ReadMetadata();
            _queueService = new MemoryQueueService
            {
                ExpireOn = 30
            };
        }

        [Theory]
        [InlineData("Test")]
        public async Task QueueAsync(string queueName)
        {
            //var queueService = new QueueService(new LiteDatabaseAsync($"{Guid.NewGuid()}.qdb") { UtcDate = true }, _logger);
            var content = Guid.NewGuid().ToString();
            var receipt = await _queueService.SendMessageAsync(queueName, content);
            receipt.ShouldNotBeNull();
        }

        [Theory]
        [InlineData("Test")]
        public async Task DequeueAsync(string queueName)
        {
            //var queueService = new QueueService(new LiteDatabaseAsync($"{Guid.NewGuid()}.qdb") { UtcDate = true }, _logger);
            var content = Guid.NewGuid().ToString();
            await _queueService.SendMessageAsync(queueName, content);
            var message = await _queueService.ReceiveMessageAsync(queueName);
            //var messages = await _queueService.PeekMessagesAsync(queueName);
            message.ShouldNotBeNull();
            message.MessageText.ShouldNotBeNullOrWhiteSpace();
            //messages.ShouldNotBeNull();
        }

        [Fact(Skip = "MemoryQueueService.DeleteMessageAsync is not implemented in this branch. Enable when behavior lands.")]
        public async Task DeleteMessageAsyncRemovesPeekedMessage()
        {
            var queueName = Guid.NewGuid().ToString();
            var deletedContent = Guid.NewGuid().ToString();
            var remainingContent = Guid.NewGuid().ToString();

            await _queueService.SendMessageAsync(queueName, deletedContent);
            await _queueService.SendMessageAsync(queueName, remainingContent);

            var message = (QueueMessage)await _queueService.PeekMessageAsync(queueName);

            await _queueService.DeleteMessageAsync(message, queueName);

            var remaining = await _queueService.ReceiveMessageAsync(queueName);
            remaining.MessageText.ShouldBe(remainingContent);
        }
    }
}
