using System;

namespace Gearbox.Core.Models
{
    public class SendReceipt
    {
        internal SendReceipt(string messageId, DateTimeOffset? insertionTime, DateTimeOffset? expirationTime, DateTimeOffset? timeNextVisible)
        {
            ArgumentNullException.ThrowIfNull(messageId);
            MessageId = messageId;
            InsertionTime = insertionTime;
            ExpirationTime = expirationTime;
            TimeNextVisible = timeNextVisible;
        }

        public DateTimeOffset? TimeNextVisible { get; private set; }

        public DateTimeOffset? ExpirationTime { get; private set; }

        public DateTimeOffset? InsertionTime { get; private set; }

        public string MessageId { get; private set; }
        public bool IsSuccess { get; set; }
    }
}
