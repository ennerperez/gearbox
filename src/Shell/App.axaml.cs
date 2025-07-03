using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Gearbox.Core;
using Gearbox.Shell.ViewModels;
using Gearbox.Shell.Views;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
#if DEBUG

// ReSharper disable UnusedAutoPropertyAccessor.Global

#else
#endif

namespace Gearbox.Shell
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var vm = new MainWindowViewModel();
            if (!Avalonia.Controls.Design.IsDesignMode)
            {

                BindingPlugins.DataValidators.RemoveAt(0);

                var services = new ServiceCollection();

                if (Program.Configuration != null)
                {
                    services.AddSingleton(Program.Configuration);
                }
#if DEBUG
                services.AddLogging(c => c.AddSerilog(Log.Logger, true));
#endif

                services
                    .AddInfrastructure()
                    .AddPersistence()
                    .AddCore().WithBackend();
                services
                    .RegisterServices()
                    .RegisterViewsModels();

                Program.Services = services.BuildServiceProvider();

                vm = Program.Services.GetRequiredService<MainWindowViewModel>();
            }

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();

                desktop.MainWindow = new MainWindow { DataContext = vm };
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                singleViewPlatform.MainView = new MainWindow { DataContext = vm };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Get an array of plugins to remove
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }
}
