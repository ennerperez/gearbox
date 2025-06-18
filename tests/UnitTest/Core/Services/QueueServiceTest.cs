using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Gearbox.Core.Services;
using Shouldly;

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
    }
}
