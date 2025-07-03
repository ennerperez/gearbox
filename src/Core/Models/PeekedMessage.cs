using System;
using System.ComponentModel;
using System.Text;

namespace Gearbox.Core.Models
{
    public class PeekedMessage
    {
        public PeekedMessage()
        {

        }
        public PeekedMessage(string messageId, byte[] body, DateTimeOffset? insertedOn = null, DateTimeOffset? expiresOn = null, long dequeueCount = 0) : this()
        {
            MessageId = messageId;
            Body = body;
            InsertedOn = insertedOn;
            ExpiresOn = expiresOn;
            DequeueCount = dequeueCount;
        }

        /// <summary>
        /// The Id of the Message.
        /// </summary>
        public string MessageId { get; internal set; } = string.Empty;

        /// <summary>
        /// The content of the Message.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string MessageText
        {
            get => Encoding.Default.GetString(Body);
            set => Body = Encoding.Default.GetBytes(value);
        }

        /// <summary>
        /// The content of the Message.
        /// </summary>
        public byte[] Body { get; internal set; } = Array.Empty<byte>();

        /// <summary>
        /// The time the Message was inserted into the Queue.
        /// </summary>
        public DateTimeOffset? InsertedOn { get; internal set; }

        /// <summary>
        /// The time that the Message will expire and be automatically deleted.
        /// </summary>
        public DateTimeOffset? ExpiresOn { get; internal set; }

        /// <summary>
        /// The number of times the message has been dequeued.
        /// </summary>
        public long DequeueCount { get; internal set; }
    }
}
