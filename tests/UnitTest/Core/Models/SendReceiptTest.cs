using System;
using Gearbox.Core.Models;
using Shouldly;

namespace Gearbox.UnitTest.Core.Models
{
    public class SendReceiptTest
    {
        [Fact]
        public void ShouldCreateANewSendReceipt()
        {
            var messageId = "test-message-id";
            var insertionTime = DateTimeOffset.UtcNow;
            var expirationTime = insertionTime.AddMinutes(5);
            var timeNextVisible = insertionTime.AddMinutes(1);

            var model = new SendReceipt(messageId, insertionTime, expirationTime, timeNextVisible);

            model.MessageId.ShouldBe(messageId);
            model.InsertionTime.ShouldBe(insertionTime);
            model.ExpirationTime.ShouldBe(expirationTime);
            model.TimeNextVisible.ShouldBe(timeNextVisible);
        }
    }
}
