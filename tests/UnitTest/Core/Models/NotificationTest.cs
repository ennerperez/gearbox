using Gearbox.Core.Models;
using Gearbox.Core.Types;
using Shouldly;

namespace Gearbox.UnitTest.Core.Models
{
    public class NotificationTest
    {
        [Fact]
        public void Should_Create_A_New_Notification()
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
        public void Should_Create_A_New_Notification_With_Type()
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
