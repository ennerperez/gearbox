using Gearbox.Core.Interfaces;
using Gearbox.Shell.Services;
using Gearbox.Shell.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Gearbox.Shell
{
    public static class Extensions
    {
        public static IServiceCollection RegisterViewsModels(this IServiceCollection services)
        {
            services.AddTransient<MainWindowViewModel>();

            return services;
        }
        public static IServiceCollection RegisterServices(this IServiceCollection services)
        {
            services.AddTransient<INotificationService, NotificationService>();

            return services;
        }
    }
}
