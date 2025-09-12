using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Gearbox.Core.Interfaces;
using Gearbox.Core.Models;
using Gearbox.Core.Types;
using Gearbox.Host.Services;

namespace Gearbox.UnitTest.Host
{
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
            await _notificationService.ShowAsync(new Notification()
            {
                Message = "Hello World!",
                Expiration = DateTime.Now.AddSeconds(7).TimeOfDay,
                Title = "Test",
                Type = NotificationType.Information,
            });
            await Task.Delay(1000);
            Assert.True(true);
        }
    }
}
