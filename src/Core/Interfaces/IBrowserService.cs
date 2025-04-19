using System.Threading.Tasks;

namespace Gearbox.Core.Interfaces
{
    public interface IBrowserService
    {
        Task<int> LaunchAsync(string url, string windowTitle = "");
    }
}
