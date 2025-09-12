using System;
using System.Threading.Tasks;
using DesktopNotifications;
using DesktopNotifications.Windows;

namespace Gearbox.Shell.Services
{
    public class WindowsNotificationManager : INotificationManager
    {
        public WindowsNotificationManager(WindowsApplicationContext context)
        {
        }

        public Task Initialize()
        {
            return Task.CompletedTask;
        }

        public Task ShowNotification(Notification notification, DateTimeOffset? expirationTime = null)
        {
            return Task.CompletedTask;
        }

        public Task HideNotification(Notification notification)
        {
            return Task.CompletedTask;
        }

        public Task ScheduleNotification(Notification notification, DateTimeOffset deliveryTime, DateTimeOffset? expirationTime = null)
        {
            return Task.CompletedTask;
        }

        public string LaunchActionId { get; }
        public NotificationManagerCapabilities Capabilities { get; }

#pragma warning disable CS0067
        public event EventHandler<NotificationActivatedEventArgs> NotificationActivated;
        public event EventHandler<NotificationDismissedEventArgs> NotificationDismissed;
#pragma warning disable CS0067

        #region IDisposable

        private void ReleaseUnmanagedResources()
        {
            // TODO release unmanaged resources here
        }

        protected virtual void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();
            if (disposing)
            {
                // TODO release managed resources here
            }
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
