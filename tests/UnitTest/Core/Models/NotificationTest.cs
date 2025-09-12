using System;
using Gearbox.Core.Models;
using Gearbox.Core.Types;
using Shouldly;

namespace Gearbox.UnitTest.Core.Models
{
    public class NotificationTest
    {
        [Fact]
        public void ShouldCreateANewNotification()
        {
            var model = new Notification()
            {
                Title = "Test Notification",
                Message = "This is a test notification message.",
            };
            model.Title.ShouldNotBeEmpty();
            model.Message.ShouldNotBeEmpty();
            model.ShouldNotBeNull();
        }

        [Fact]
        public void ShouldCreateANewNotificationWithType()
        {
            var model = new Notification("Test Title", "This is a test message.")
            {
                Type = NotificationType.Information,
                Expiration = TimeSpan.FromSeconds(10),
                OnClick = () => { /* Click action */ },
                OnClose = () => { /* Close action */ }
            };
            model.Title.ShouldNotBeEmpty();
            model.Message.ShouldNotBeEmpty();
            model.Type.ShouldBe(NotificationType.Information);
            model.Expiration.ShouldNotBeNull();
            model.OnClick.ShouldNotBeNull();
            model.OnClose.ShouldNotBeNull();
        }
    }
}
