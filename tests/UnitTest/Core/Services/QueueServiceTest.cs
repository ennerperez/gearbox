using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

        [Fact]
        public async Task DeleteMessageAsyncRemovesMatchingMessageAndKeepsRemainingOrder()
        {
            var queueName = Guid.NewGuid().ToString();
            var firstContent = Guid.NewGuid().ToString();
            var secondContent = Guid.NewGuid().ToString();

            await _queueService.SendMessageAsync(queueName, firstContent);
            await _queueService.SendMessageAsync(queueName, secondContent);

            var message = (QueueMessage)await _queueService.PeekMessageAsync(queueName);
            await _queueService.DeleteMessageAsync(message, queueName);

            var remainingMessage = await _queueService.ReceiveMessageAsync(queueName);
            var emptyMessage = await _queueService.ReceiveMessageAsync(queueName);

            remainingMessage.ShouldNotBeNull();
            remainingMessage.MessageText.ShouldBe(secondContent);
            emptyMessage.ShouldBeNull();
        }

        [Fact]
        public async Task PeekMessagesAsyncHonorsMaxMessagesWithoutDequeuing()
        {
            var queueName = Guid.NewGuid().ToString();

            await _queueService.SendMessageAsync(queueName, Guid.NewGuid().ToString());
            await _queueService.SendMessageAsync(queueName, Guid.NewGuid().ToString());

            var peekedMessages = await _queueService.PeekMessagesAsync(queueName, 1);
            var receivedMessages = await _queueService.ReceiveMessagesAsync(queueName, 2);

            peekedMessages.Count().ShouldBe(1);
            receivedMessages.Count().ShouldBe(2);
        }

        [Fact]
        public async Task ReceiveMessagesAsyncHonorsMaxMessages()
        {
            var queueName = Guid.NewGuid().ToString();

            await _queueService.SendMessageAsync(queueName, Guid.NewGuid().ToString());
            await _queueService.SendMessageAsync(queueName, Guid.NewGuid().ToString());

            var receivedMessages = await _queueService.ReceiveMessagesAsync(queueName, 1);
            var remainingMessage = await _queueService.ReceiveMessageAsync(queueName);

            receivedMessages.Count().ShouldBe(1);
            remainingMessage.ShouldNotBeNull();
        }
    }
}
