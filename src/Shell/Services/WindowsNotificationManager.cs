using System;
using System.Threading.Tasks;
using DesktopNotifications;
using DesktopNotifications.Windows;

namespace Gearbox.Shell.Services
{
    public class WindowsNotificationManager : INotificationManager
    {
        private readonly DesktopNotifications.Windows.WindowsNotificationManager _manager;
        private bool _disposed;

        public WindowsNotificationManager(WindowsApplicationContext context)
        {
            _manager = new DesktopNotifications.Windows.WindowsNotificationManager(context);
        }

        public Task Initialize()
        {
            return _manager.Initialize();
        }

        public Task ShowNotification(Notification notification, DateTimeOffset? expirationTime = null)
        {
            return _manager.ShowNotification(notification, expirationTime);
        }

        public Task HideNotification(Notification notification)
        {
            return _manager.HideNotification(notification);
        }

        public Task ScheduleNotification(Notification notification, DateTimeOffset deliveryTime, DateTimeOffset? expirationTime = null)
        {
            return _manager.ScheduleNotification(notification, deliveryTime, expirationTime);
        }

        public string LaunchActionId => _manager.LaunchActionId;
        public NotificationManagerCapabilities Capabilities => _manager.Capabilities;

        public event EventHandler<NotificationActivatedEventArgs> NotificationActivated
        {
            add => _manager.NotificationActivated += value;
            remove => _manager.NotificationActivated -= value;
        }

        public event EventHandler<NotificationDismissedEventArgs> NotificationDismissed
        {
            add => _manager.NotificationDismissed += value;
            remove => _manager.NotificationDismissed -= value;
        }

        #region IDisposable

        private void ReleaseUnmanagedResources()
        {
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            ReleaseUnmanagedResources();
            if (disposing)
            {
                _manager.Dispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~WindowsNotificationManager()
        {
            Dispose(false);
        }

        #endregion
    }
}
