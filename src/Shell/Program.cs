using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Avalonia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using OS = System.Runtime.OperatingSystemExtensions;
#if DEBUG

// ReSharper disable UnusedAutoPropertyAccessor.Global

#else
#endif

namespace Gearbox.Shell
{
    sealed class Program
    {
        internal static IServiceProvider? Services { get; set; }
        internal static IConfiguration? Configuration { get; set; }

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            Assembly.GetEntryAssembly()?.ReadMetadata();

            var mutex = new Mutex(true, Metadata.Product, out var result);
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

#if DEBUG
            // Initialize Logger
            var loggerConfiguration = new LoggerConfiguration()
                .WriteTo.Async(a =>
                {
                    a.Console();
                    a.File(
                        path: Path.Combine(OS.GetDataDir(), "Logs/.log"),
                        rollingInterval: RollingInterval.Day,
                        flushToDiskInterval: TimeSpan.FromSeconds(30),
                        shared: true
                    );
                })
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithProcessName()
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .Enrich.WithProperty("ApplicationName", Metadata.Product);

            // Initialize Logger
            Log.Logger = loggerConfiguration
                .WriteTo.Trace()
                .CreateLogger();
#endif

            System.Runtime.Exceptions.UnhandledException += (_, e) =>
            {
                var logger = Services?.GetService<ILoggerFactory>()?.CreateLogger(typeof(Program));
                var ex = e.ExceptionObject as Exception;
                logger?.LogCritical(ex, "{Message}", ex?.Message);
            };

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);

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
