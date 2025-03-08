using System;
using Gearbox.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Gearbox.Core
{
  public static class Extensions
  {
    public static IServiceCollection AddCore(this IServiceCollection serviceCollection)
    {
      if (OperatingSystem.IsWindows())
      {
        serviceCollection.AddSingleton<IBackend, Natives.Windows.Backend>();
      }
      else if (OperatingSystem.IsMacOS())
      {
        serviceCollection.AddSingleton<IBackend, Natives.MacOS.Backend>();
      }
      else if (OperatingSystem.IsLinux())
      {
        serviceCollection.AddSingleton<IBackend, Natives.Linux.Backend>();
      }
      serviceCollection.AddSingleton<IBrowserService, Services.BrowserService>();
      return serviceCollection;
    }
  }
}
