using System;
using Gearbox.Core.Interfaces;
using Gearbox.Core.Models;

namespace Gearbox.Shell.Services
{
    public class NotificationService : INotificationService
    {
        public void Show(Notification notification)
        {
            Console.WriteLine($"Notification: {notification.Title} - {notification.Message}");
        }
    }
}
