using System.Threading.Tasks;
using Gearbox.Core.Types;

namespace Gearbox.Core.Interfaces
{
    public interface IBackend
    {
        string GetActiveWindowTitle();
        RegisterStatus GetRegisterStatus();
        Task<bool> RegisterAsync();
        Task<bool> UnregisterAsync();
        Task<bool> RegisterOrUnregisterAsync();
        void OpenSettings();
    }
}
