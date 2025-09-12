using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gearbox.Core.Interfaces
{
    public interface IBrowserService
    {
        Task<bool> LaunchAsync(string url, string windowTitle = "");
        Task<bool> LaunchAsync(Uri url, string windowTitle = "");
        Task<bool> LaunchAsync(IBrowser browser, string url);
        Task<bool> LaunchAsync(IBrowser browser, Uri url);

        IDictionary<string, T> GetBrowsers<T>() where T : IBrowser;
    }
}
