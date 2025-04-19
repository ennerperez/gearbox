using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gearbox.Core.Models;

namespace Gearbox.Core.Interfaces
{
    public interface IQueueService
    {
        Task<PeekedMessage?> PeekMessageAsync(string queueName = "", CancellationToken cancellationToken = default);
        Task<IEnumerable<PeekedMessage>?> PeekMessagesAsync(string queueName = "", int? maxMessages = default, CancellationToken cancellationToken = default);
        Task<QueueMessage?> ReceiveMessageAsync(string queueName = "", TimeSpan? visibilityTimeout = default, CancellationToken cancellationToken = default);
        Task<IEnumerable<QueueMessage>?> ReceiveMessagesAsync(string queueName = "", int? maxMessages = default, TimeSpan? visibilityTimeout = default, CancellationToken cancellationToken = default);
        Task<SendReceipt?> SendMessageAsync(string queueName = "", string content = "", CancellationToken cancellationToken = default);
        Task DeleteMessageAsync(QueueMessage message, string queueName = "", CancellationToken cancellationToken = default);
    }
}
