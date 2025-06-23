using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Gearbox.Core;
using Gearbox.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OS = System.Runtime.OperatingSystemExtensions;
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
        [STAThread]
        public static async Task Main(string[] args)
        {
            Assembly.GetEntryAssembly()?.ReadMetadata();

            var builder = BuildRunnerApp(args);
            var host = builder.Build();
            var backend = host.Services.GetService<IBackend>();
            var logger = host.Services.GetService<ILoggerFactory>()?.CreateLogger(typeof(Program));
            backend?.StartHost();

            var queueService = host.Services.GetService<IQueueService>();
            var result = await queueService?.SendMessageAsync(Metadata.Product ?? "Gearbox", string.Join(" ", args), CancellationToken.None)!;
            if (result == null || result.IsSuccess == false)
            {
                logger?.LogError("Failed to send message to the queue.");
            }

            Environment.Exit(1);
            return;
        }

        private static HostApplicationBuilder BuildRunnerApp(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            var assemblyPath = Path.GetDirectoryName(AppContext.BaseDirectory);
            var configuration = new ConfigurationBuilder()
                .SetBasePath(assemblyPath ?? Directory.GetCurrentDirectory())
                .AddIniFile("Config.ini")
                .AddIniFile($"Config.{OS.GetName()}.ini", true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            // Register all the services needed for the application to run
            builder.Services.AddSingleton(configuration);
#if DEBUG
            builder.Services.AddLogging(c => c.AddSerilog(Log.Logger, true));
#endif

            // Core Services
            builder.Services
                .AddInfrastructure()
                .AddPersistence()
                //.AddCore()
                .AddRunner();

            return builder;
        }
    }
}
