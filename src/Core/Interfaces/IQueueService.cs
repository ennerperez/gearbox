using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gearbox.Core.Models;

namespace Gearbox.Core.Interfaces
{
    public interface IQueueService
    {
        Task<PeekedMessage> PeekMessageAsync(string queueName = "", CancellationToken cancellationToken = default);
        Task<IEnumerable<PeekedMessage>> PeekMessagesAsync(string queueName = "", int? maxMessages = null, CancellationToken cancellationToken = default);
        Task<QueueMessage> ReceiveMessageAsync(string queueName = "", TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default);
        Task<IEnumerable<QueueMessage>> ReceiveMessagesAsync(string queueName = "", int? maxMessages = null, TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default);
        Task<SendReceipt> SendMessageAsync(string queueName = "", string content = "", CancellationToken cancellationToken = default);
        Task DeleteMessageAsync(QueueMessage message, string queueName = "", CancellationToken cancellationToken = default);

        double ExpireOn { get; set; }

    }
}
