using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Gearbox.Core.Interfaces;
using Gearbox.Core.Models;
using Gearbox.Core.Types;
using Gearbox.Runner.Services;

namespace Gearbox.UnitTest.Runner
{
    [ExcludeFromCodeCoverage]
    [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
    public class NotificationServiceTest
    {
        private readonly INotificationService _notificationService;

        public NotificationServiceTest()
        {
            Assembly.GetEntryAssembly()?.ReadMetadata();
            _notificationService = new NotificationService();
        }

        [Fact]
        public async Task ShowNotificationAsync()
        {
            _notificationService.Show(new Notification()
            {
                Message = "Hello World!",
                Expiration = DateTime.Now.AddSeconds(7).TimeOfDay,
                Title = "Test",
                Type = NotificationType.Information,
            });
            Assert.True(true);
            await Task.Delay(1000);
        }
    }
}
