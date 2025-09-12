using System.Threading.Tasks;
using Gearbox.Core.Interfaces;
using Gearbox.Core.Models;

namespace Gearbox.Host.Services
{
    public class NotificationService : INotificationService
    {
        public Task ShowAsync(Notification notification)
        {
            return Task.CompletedTask;
        }

    }
}
