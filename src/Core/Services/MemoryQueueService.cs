using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Gearbox.Core.Interfaces;
using Gearbox.Core.Models;

namespace Gearbox.Core.Services
{
    public class MemoryQueueService : IQueueService
    {
        private static readonly Dictionary<string, Channel<object>> s_channel = new();

        public double ExpireOn { get; set; } = 3600; // Default expiration time in seconds (1 hour)

        public Task<PeekedMessage> PeekMessageAsync(string queueName = "", CancellationToken cancellationToken = default)
        {
            if (!s_channel.TryGetValue(queueName, out Channel<object> value))
            {
                value = Channel.CreateUnbounded<object>();
                s_channel.Add(queueName, value);
            }

            value.Reader.TryPeek(out var message);
            return Task.FromResult((PeekedMessage)message);
        }

        public Task<IEnumerable<PeekedMessage>> PeekMessagesAsync(string queueName = "", int? maxMessages = null, CancellationToken cancellationToken = default)
        {
            if (!s_channel.TryGetValue(queueName, out Channel<object> value))
            {
                value = Channel.CreateUnbounded<object>();
                s_channel.Add(queueName, value);
            }

            var result = new List<PeekedMessage>();
            while (value.Reader.Count > 0)
            {
                value.Reader.TryPeek(out var message);
                if (message != null)
                {
                    result.Add((PeekedMessage)message);
                }
            }

            return Task.FromResult(result.AsEnumerable());
        }

        public Task<QueueMessage> ReceiveMessageAsync(string queueName = "", TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default)
        {
            if (!s_channel.TryGetValue(queueName, out Channel<object> value))
            {
                value = Channel.CreateUnbounded<object>();
                s_channel.Add(queueName, value);
            }

            value.Reader.TryRead(out var message);

            return Task.FromResult((QueueMessage)message);
        }

        public Task<IEnumerable<QueueMessage>> ReceiveMessagesAsync(string queueName = "", int? maxMessages = null, TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default)
        {
            if (!s_channel.TryGetValue(queueName, out Channel<object> value))
            {
                value = Channel.CreateUnbounded<object>();
                s_channel.Add(queueName, value);
            }

            var result = new List<QueueMessage>();
            while (value.Reader.Count > 0)
            {
                value.Reader.TryRead(out var message);
                if (message != null)
                {
                    result.Add((QueueMessage)message);
                }
            }

            return Task.FromResult(result.AsEnumerable());
        }

        public Task<SendReceipt> SendMessageAsync(string queueName = "", string content = "", CancellationToken cancellationToken = default)
        {
            if (!s_channel.TryGetValue(queueName, out Channel<object> value))
            {
                value = Channel.CreateUnbounded<object>();
                s_channel.Add(queueName, value);
            }

            var message = new QueueMessage(content)
            {
                InsertedOn = DateTimeOffset.Now,
                ExpiresOn = DateTimeOffset.Now.AddSeconds(ExpireOn)
            };
            value.Writer.TryWrite(message);

            return Task.FromResult<SendReceipt>(new SendReceipt(message.MessageId, message.InsertedOn, message.ExpiresOn, message.NextVisibleOn) { IsSuccess = true });
        }

        public Task DeleteMessageAsync(QueueMessage message, string queueName = "", CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
