using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Gearbox.Shell.ViewModels;
using Gearbox.Shell.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Gearbox.Shell
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public Window ActiveWindow
        {
            get
            {
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    return desktop.MainWindow;
                }

                return null;
            }
        }

        public override void OnFrameworkInitializationCompleted()
        {
            MainWindow window = null;
            if (!Design.IsDesignMode)
            {
                BindingPlugins.DataValidators.RemoveAt(0);

                if (Program.Services != null)
                {
                    Program.Services.GetRequiredService<MainWindowViewModel>();
                    window = Program.Services.GetRequiredService<MainWindow>();
                }
            }
#if DEBUG

            else
            {
                window = new MainWindow(new MainWindowViewModel());
            }
#endif

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();

                desktop.MainWindow = window;
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                singleViewPlatform.MainView = window;
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
