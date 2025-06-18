using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Gearbox.Core;
using Gearbox.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BackgroundService = Gearbox.Runner.Services.BackgroundService;
using OS = System.Runtime.OperatingSystemExtensions;
using ILogger = Microsoft.Extensions.Logging.ILogger;
#if DEBUG
using Serilog;

// ReSharper disable UnusedAutoPropertyAccessor.Global

#else
#endif

namespace Gearbox.Runner
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        private static IServiceProvider? Services { get; set; }
        private static IConfiguration? Configuration { get; set; }

        [STAThread]
        public static void Main(string[] args)
        {
            Assembly.GetEntryAssembly()?.ReadMetadata();

            var builder = BuildRunnerApp(args);
            var host = builder.Build();
            Services = host.Services;

            var isCommand = BackgroundService.Commands.Join(args, s => s, s => s, (s1, s2) => s1 == s2).Any(m => m);
            var mutex = new Mutex(true, Metadata.Product, out var result);

            if (!result && !isCommand)
            {
                var queueService = Services.GetService<IQueueService>();
                queueService?.SendMessageAsync(Metadata.Product ?? "Gearbox", string.Join(" ", args));
                Environment.Exit(1);
                return;
            }

#if DEBUG
            // Initialize Logger
            var loggerConfiguration = new LoggerConfiguration()
                .WriteTo.Async(a =>
                {
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

            ILogger? logger = null;
            try
            {
                IsRunning = true;

                logger = Services.GetService<ILoggerFactory>()?.CreateLogger(typeof(Program));

                if (isCommand)
                {
                    host.StartAsync();
                    return;
                }

                host.Run();
            }
            catch (Exception e)
            {
                logger?.LogCritical(e, "{Message}", e.Message);
            }
            finally
            {
                IsRunning = false;
            }

            GC.KeepAlive(mutex);
        }

        private static HostApplicationBuilder BuildRunnerApp(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            var assemblyPath = Path.GetDirectoryName(AppContext.BaseDirectory);
            Configuration = new ConfigurationBuilder()
                .SetBasePath(assemblyPath ?? Directory.GetCurrentDirectory())
                .AddIniFile("Config.ini")
                .AddIniFile($"Config.{OS.GetName()}.ini", true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            // Register all the services needed for the application to run
            builder.Services.AddSingleton(Configuration);
#if DEBUG
            builder.Services.AddLogging(c => c.AddSerilog(Log.Logger, true));
#endif

            // Core Services
            builder.Services
                .AddInfrastructure()
                .AddPersistence()
                .AddCore()
                .AddRunner();

            builder.Services.AddHostedService<BackgroundService>();

            return builder;
        }

        internal static bool IsRunning { get; private set; }
    }
}
