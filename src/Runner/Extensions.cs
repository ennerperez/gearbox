using System.Diagnostics.CodeAnalysis;
using Gearbox.Core.Interfaces;
using Gearbox.Runner.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Gearbox.Runner
{
    [ExcludeFromCodeCoverage]
    public static class Extensions
    {
        public static IServiceCollection AddRunner(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<INotificationService, NotificationService>();
            return serviceCollection;
        }
    }
}
