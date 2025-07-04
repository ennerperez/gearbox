using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using DesktopNotifications;
using DesktopNotifications.FreeDesktop;
using DesktopNotifications.Windows;
using Gearbox.Core.Interfaces;
using Gearbox.Shell.Services;
using Gearbox.Shell.ViewModels;
using Gearbox.Shell.Views;
using Microsoft.Extensions.DependencyInjection;
using WindowsNotificationManager = Gearbox.Shell.Services.WindowsNotificationManager;

namespace Gearbox.Shell
{
    public static partial class Extensions
    {
        public static IServiceCollection RegisterViews(this IServiceCollection services)
        {
            services.AddTransient<MainWindow>();
            services.AddTransient<AboutWindow>();

            return services;
        }
        public static IServiceCollection RegisterViewsModels(this IServiceCollection services)
        {
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<AboutWindowViewModel>();

            return services;
        }
        public static IServiceCollection RegisterServices(this IServiceCollection services)
        {
            INotificationManager manager;
            if (OperatingSystem.IsWindows())
            {
                var context = WindowsApplicationContext.FromCurrentProcess();
                manager = new WindowsNotificationManager(context);
                services.AddSingleton(manager);
            }
            else if (OperatingSystem.IsLinux())
            {
                var context = FreeDesktopApplicationContext.FromCurrentProcess();
                manager = new FreeDesktopNotificationManager(context);
                services.AddSingleton(manager);
            }
            else
            {
                //TODO: OSX once implemented/stable
            }

            services.AddTransient<INotificationService, NotificationService>();

            return services;
        }

        public static AppBuilder? WithDesktopNotifications(this AppBuilder? builder)
        {
            if (Program.Services == null)
            {
                return builder;
            }

            var manager = Program.Services.GetService<INotificationManager>();
            builder?.AfterSetup(b =>
            {
                if (b.Instance?.ApplicationLifetime is IControlledApplicationLifetime lifetime)
                {
                    lifetime.Exit += (s, _) =>
                    {
                        manager?.Dispose();
                    };
                }
            });

            return builder;
        }
        public static async Task StartWithOpenerLifetime(this AppBuilder builder, string[] args)
        {
            var browserService = Program.Services?.GetService<IBrowserService>();
            var backend = Program.Services?.GetService<IBackend>();
            var arg = string.Join(" ", args);
            var registerCommandRegexMatch = RegisterCommandRegex().Match(arg);
            var unregisterCommandRegexMatch = UnregisterCommandRegex().Match(arg);
            var forceSourceCommandRegexMatch = ForceSourceCommandRegex().Match(arg);
            if (forceSourceCommandRegexMatch.Success)
            {
                var url = forceSourceCommandRegexMatch.Groups[1].Value;
                var source = forceSourceCommandRegexMatch.Groups[2].Value;
                if (browserService == null) { throw new OperationCanceledException("Browser Service was not found"); }
                await browserService.LaunchAsync(url, source);
                Environment.Exit(0);
            }
            else if (registerCommandRegexMatch.Success)
            {
                if (backend == null) { throw new OperationCanceledException("Backend Service was not found"); }
                await backend.RegisterAsync();
                Environment.Exit(0);
            }
            else if (unregisterCommandRegexMatch.Success)
            {
                if (backend == null) { throw new OperationCanceledException("Backend Service was not found"); }
                await backend.UnregisterAsync();
                Environment.Exit(0);
            }
            else
            {
                if (browserService == null) { throw new OperationCanceledException("Browser Service was not found"); }
                await browserService.LaunchAsync(arg);
                Environment.Exit(0);
            }

        }

        #region Commands

        private static readonly string[] s_registerCommand = ["--register", "-r"];
        private static readonly string[] s_unregisterCommand = ["--unregister", "-u"];

        [GeneratedRegex("\\-r|\\-\\-register", RegexOptions.Compiled)]
        private static partial Regex RegisterCommandRegex();

        [GeneratedRegex("\\-u|\\-\\-unregister", RegexOptions.Compiled)]
        private static partial Regex UnregisterCommandRegex();

        [GeneratedRegex("(.*) [\\-fS|\\-\\-forceSource]{1,} (.*)", RegexOptions.Compiled)]
        private static partial Regex ForceSourceCommandRegex();

        public static readonly IEnumerable<string> Commands = s_registerCommand.Concat(s_unregisterCommand).ToArray();

        #endregion

    }
}
