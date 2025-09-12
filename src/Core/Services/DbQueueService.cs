using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gearbox.Core.Interfaces;
using Gearbox.Core.Models;
using LiteDB.Async;
using Microsoft.Extensions.Logging;

namespace Gearbox.Core.Services
{
    public class DbQueueService : IQueueService
    {
        private readonly ILogger _logger;
        private readonly ILiteDatabaseAsync _database;

        public double ExpireOn { get; set; }

        public DbQueueService(ILiteDatabaseAsync database, ILogger<IQueueService> logger)
        {
            _database = database;
            _logger = logger;
        }

        public async Task<PeekedMessage> PeekMessageAsync(string queueName = "", CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(queueName)) { return null; }

            var collection = await PrepareCollectionAsync(queueName);

            var items = await collection.FindAsync(p => (p.ExpiresOn == null || p.ExpiresOn >= DateTime.UtcNow) && (p.NextVisibleOn == null || p.NextVisibleOn <= DateTime.UtcNow));
            if (items == null) { return null; }

            var message = items.OrderByDescending(m => m.InsertedOn).FirstOrDefault();
            if (message != null) { return await Task.FromResult(message); }

            return null;
        }

        public async Task<IEnumerable<PeekedMessage>> PeekMessagesAsync(string queueName = "", int? maxMessages = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(queueName)) { return []; }

            var collection = await PrepareCollectionAsync(queueName);

            var items = await collection.FindAsync(p => (p.ExpiresOn == null || p.ExpiresOn >= DateTime.UtcNow) && (p.NextVisibleOn == null || p.NextVisibleOn <= DateTime.UtcNow));
            if (items == null) { return []; }

            var messages = items.OrderByDescending(m => m.InsertedOn).Take(maxMessages ?? 1);
            var peekMessagesAsync = messages as PeekedMessage[] ?? messages.ToArray();
            if (peekMessagesAsync.Length != 0) { return await Task.FromResult(peekMessagesAsync); }

            return [];
        }

        private async Task<ILiteCollectionAsync<QueueMessage>> PrepareCollectionAsync(string queueName)
        {
            var collection = _database.GetCollection<QueueMessage>(queueName);
            await collection.EnsureIndexAsync(p => p.InsertedOn);
            await collection.EnsureIndexAsync(p => p.ExpiresOn);
            await collection.EnsureIndexAsync(p => p.NextVisibleOn);
            return collection;
        }

        public async Task<QueueMessage> ReceiveMessageAsync(string queueName = "", TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(queueName)) { return null; }

            var collection = await PrepareCollectionAsync(queueName);

            var items = await collection.FindAsync(p => (p.ExpiresOn == null || p.ExpiresOn >= DateTime.UtcNow) && (p.NextVisibleOn == null || p.NextVisibleOn <= DateTime.UtcNow));
            if (items == null) { return null; }

            var message = items.OrderByDescending(m => m.InsertedOn).FirstOrDefault();
            if (message == null) { return null; }

            try
            {
                if (visibilityTimeout != null)
                {
                    message.NextVisibleOn = DateTime.UtcNow.Add(visibilityTimeout.Value);
                    await collection.UpsertAsync(message.MessageId, message);
                }
                else
                {
                    var ids = new[] { message.MessageId };
                    await collection.DeleteManyAsync(m => ids.Contains(m.MessageId));
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "{Message}", e.Message);
            }

            return await Task.FromResult(message);
        }

        public async Task<IEnumerable<QueueMessage>> ReceiveMessagesAsync(string queueName = "", int? maxMessages = null, TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(queueName)) { return []; }

            var collection = await PrepareCollectionAsync(queueName);

            var items = await collection.FindAsync(p => (p.ExpiresOn == null || p.ExpiresOn >= DateTime.UtcNow) && (p.NextVisibleOn == null || p.NextVisibleOn <= DateTime.UtcNow));
            if (items == null) { return []; }

            var messages = items.OrderByDescending(m => m.InsertedOn).Take(maxMessages ?? 1).ToArray();
            if (messages.Length == 0) { return []; }

            var ids = messages.Select(m => m.MessageId).ToArray();
            try
            {
                if (visibilityTimeout != null)
                {
                    foreach (var message in messages)
                    {
                        message.NextVisibleOn = DateTime.UtcNow.Add(visibilityTimeout.Value);
                    }

                    await collection.UpsertAsync(messages);
                }
                else
                {
                    await collection.DeleteManyAsync(m => ids.Contains(m.MessageId));
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "{Message}", e.Message);
            }

            return await Task.FromResult(messages);
        }

        public async Task<SendReceipt> SendMessageAsync(string queueName = "", string content = "", CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(queueName)) { return null; }

            bool success;
            var collection = await PrepareCollectionAsync(queueName);

            var message = new QueueMessage()
            {
                MessageId = Guid.NewGuid().ToString(), MessageText = content, ExpiresOn = DateTimeOffset.UtcNow.AddHours(1), InsertedOn = DateTimeOffset.UtcNow,
            };
            try
            {
                await collection.InsertAsync(message);
                success = true;
            }
            catch (Exception e)
            {
                success = false;
                _logger.LogError(e, "{Message}", e.Message);
            }

            return await Task.FromResult(new SendReceipt(message.MessageId, message.InsertedOn, message.ExpiresOn, null) { IsSuccess = success });
        }

        public async Task DeleteMessageAsync(QueueMessage message, string queueName = "", CancellationToken cancellationToken = default)
        {
            if (message == null || string.IsNullOrWhiteSpace(queueName)) { return; }

            var collection = await PrepareCollectionAsync(queueName);

            try
            {
                var ids = new[] { message.MessageId };
                await collection.DeleteManyAsync(m => ids.Contains(m.MessageId));
                await Task.CompletedTask;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{Message}", e.Message);
            }
        }
    }
}
