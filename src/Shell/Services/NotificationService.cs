using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DesktopNotifications;
using Gearbox.Core.Interfaces;
using Notification = Gearbox.Core.Models.Notification;

namespace Gearbox.Shell.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationManager _notificationManager;

        public NotificationService(INotificationManager notificationManager)
        {
            _notificationManager = notificationManager;
            _notificationManager.NotificationDismissed += NotificationManagerOnNotificationDismissed;
            _notificationManager.NotificationActivated += NotificationManagerOnNotificationActivated;
        }

        private void NotificationManagerOnNotificationActivated(object sender, NotificationActivatedEventArgs e)
        {
            var result = s_cache.TryPop(out var item);
            if (!result) { return; }

            if (e.ActionId == "OK")
            {
                item?.OnClick?.Invoke();
            }
            else if (e.ActionId == "CLOSE")
            {
                item?.OnClose?.Invoke();
            }
        }

        private void NotificationManagerOnNotificationDismissed(object sender, NotificationDismissedEventArgs e)
        {

        }

        private static Stack<Notification> s_cache = new();

        public async Task ShowAsync(Notification notification)
        {
            if (!IsInitialized)
            {
                await _notificationManager.Initialize();
                IsInitialized = true;
            }

            s_cache.Push(notification);
            var item = new DesktopNotifications.Notification()
            {
                Title = notification.Title,
                Body = notification.Message
            };
            if (notification.OnClick != null)
            {
                item.Buttons.Add(new ValueTuple<string, string>("Ok", "OK"));
            }
            if (notification.OnClose != null)
            {
                item.Buttons.Add(new ValueTuple<string, string>("Close", "CLOSE"));
            }

            await _notificationManager.ShowNotification(item, DateTimeOffset.Now.AddSeconds(5));
        }

        public bool IsInitialized { get; private set; }
    }
}
