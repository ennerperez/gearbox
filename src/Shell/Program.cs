using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Gearbox.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OS = System.Runtime.OperatingSystemExtensions;
using Serilog;

namespace Gearbox.Shell
{
    internal sealed class Program
    {
        internal static IServiceProvider Services { get; set; }
        internal static IConfiguration Configuration { get; private set; }
        internal static AppBuilder Builder { get; private set; }

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static async Task Main(string[] args)
        {
            Assembly.GetEntryAssembly()?.ReadMetadata();

            var mutex = new Mutex(true, AssemblyMetadata.Product, out var result);
            if (!result)
            {
                Environment.Exit(1);
                return;
            }

            var assemblyPath = Path.GetDirectoryName(AppContext.BaseDirectory);
            Configuration = new ConfigurationBuilder()
                .SetBasePath(assemblyPath ?? Directory.GetCurrentDirectory())
                .AddIniFile("Config.ini")
                .AddIniFile($"Config.{OS.GetName()}.ini", true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            // Initialize Logger
            var loggerConfiguration = new LoggerConfiguration()
                .WriteTo.Async(a =>
                {
                    a.Console(formatProvider: CultureInfo.CurrentCulture);
                    a.File(
                        path: Path.Combine(OS.GetDataDir(), "Logs/.log"),
                        rollingInterval: RollingInterval.Day,
                        flushToDiskInterval: TimeSpan.FromSeconds(30),
                        shared: true,
                        formatProvider: CultureInfo.CurrentCulture
                    );
                })
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithProcessName()
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .Enrich.WithProperty("ApplicationName", AssemblyMetadata.Product);

            // Initialize Logger
            Log.Logger = loggerConfiguration
                .WriteTo.Trace(formatProvider: CultureInfo.CurrentCulture)
                .CreateLogger();

            System.Runtime.ExceptionHandler.UnhandledException += (_, e) =>
            {
                var logger = Services?.GetService<ILoggerFactory>()?.CreateLogger(typeof(Program));
                var ex = e.ExceptionObject as Exception;
                logger?.LogCritical(ex, "{Message}", ex?.Message);
            };

            var services = new ServiceCollection();

            services.AddSingleton(Configuration);
            services.AddLogging(c => c.AddSerilog(Log.Logger, true));

            services
                .AddInfrastructure()
                .AddPersistence()
                .AddCore().WithBackend();

            services
                .RegisterServices()
                .RegisterViews()
                .RegisterViewsModels();

            Services = services.BuildServiceProvider();

            Builder = BuildAvaloniaApp()
                .WithDesktopNotifications();

            if (Builder != null)
            {
                if (args.Length != 0)
                {
                    await Builder.StartWithOpenerLifetime(args);
                }

                Builder.StartWithClassicDesktopLifetime(args);
            }

            GC.KeepAlive(mutex);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
        {
            GC.KeepAlive(typeof(Avalonia.Svg.Skia.SvgImageExtension).Assembly);
            GC.KeepAlive(typeof(Avalonia.Svg.Skia.Svg).Assembly);
            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
        }
    }
}
