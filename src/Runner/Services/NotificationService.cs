using System;
using System.Threading.Tasks;
using Gearbox.Core.Interfaces;
using Gearbox.Core.Models;

namespace Gearbox.Runner.Services
{
    public class NotificationService : INotificationService
    {
        public Task ShowAsync(Notification notification)
        {
            Console.WriteLine($"Notification: {notification.Title} - {notification.Message}");
            return Task.CompletedTask;
        }

    }
}
