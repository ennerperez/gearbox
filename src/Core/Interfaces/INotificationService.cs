using System.Threading.Tasks;
using Gearbox.Core.Models;

namespace Gearbox.Core.Interfaces
{
    public interface INotificationService
    {
        Task ShowAsync(Notification notification);
    }
}
