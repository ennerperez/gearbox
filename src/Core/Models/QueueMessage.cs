using System;

namespace Gearbox.Core.Models
{
    public class QueueMessage : PeekedMessage
    {
        internal QueueMessage() { }

        internal QueueMessage(string messageText)
        {
            MessageId = Guid.NewGuid().ToString();
            MessageText = messageText;
        }

        /// <summary>
        /// The time that the message will again become visible in the Queue.
        /// </summary>
        public DateTimeOffset? NextVisibleOn { get; internal set; }

        internal static PeekedMessage ToPeekedMessage(QueueMessage message)
        {
            return new PeekedMessage()
            {
                MessageId = message.MessageId,
                DequeueCount = message.DequeueCount,
                Body = message.Body,
                ExpiresOn = message.ExpiresOn,
                InsertedOn = message.InsertedOn
            };
        }
    }
}
