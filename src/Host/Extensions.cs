using System.Diagnostics.CodeAnalysis;
using Gearbox.Core.Interfaces;
using Gearbox.Host.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Gearbox.Host
{
    [ExcludeFromCodeCoverage]
    public static class Extensions
    {
        public static IServiceCollection AddHost(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<INotificationService, NotificationService>();
            return serviceCollection;
        }
    }
}
