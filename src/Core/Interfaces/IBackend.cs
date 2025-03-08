using System.Threading.Tasks;
using Gearbox.Core.Types;

namespace Gearbox.Core.Interfaces
{
  public interface IBackend
  {
    string GetActiveWindowTitle();
    RegisterStatus GetRegisterStatus();
    Task RegisterAsync();
    Task UnregisterAsync();
    Task RegisterOrUnregisterAsync();
    void OpenSettings();

  }
}
