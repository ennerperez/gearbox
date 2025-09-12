using System;
using Gearbox.Core.Models;
using Shouldly;

namespace Gearbox.UnitTest.Core.Models
{
    public class QueueMessageTest
    {
        [Fact]
        public void ShouldCreateANewQueueMessage()
        {
            //var messageId = "test-message-id";
            var body = "This is a test message."u8.ToArray();
            var insertedOn = DateTimeOffset.UtcNow;
            var expiresOn = insertedOn.AddMinutes(5);
            var dequeueCount = 1;

            var model = new QueueMessage()
            {
                Body = body,
                InsertedOn = insertedOn,
                ExpiresOn = expiresOn,
                DequeueCount = dequeueCount
            };

            //model.MessageId.ShouldBe(messageId);
            model.Body.ShouldBe(body);
            model.InsertedOn.ShouldBe(insertedOn);
            model.ExpiresOn.ShouldBe(expiresOn);
            model.DequeueCount.ShouldBe(dequeueCount);
            model.MessageText.ShouldBe("This is a test message.");
        }
    }
}
