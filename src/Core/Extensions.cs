using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using Gearbox.Core.Interfaces;
using Gearbox.Core.Services;
using LiteDB;
using LiteDB.Async;
using Microsoft.Extensions.DependencyInjection;
using OS = System.Runtime.OperatingSystemExtensions;

namespace Gearbox.Core
{
    [ExcludeFromCodeCoverage]
    public static class Extensions
    {
        public static IServiceCollection AddPersistence(this IServiceCollection serviceCollection)
        {
            var cs = new ConnectionString(Path.Combine(OS.GetDataDir(), $"{Metadata.Product ?? "Gearbox"}.qdb"))
            {
                Connection = ConnectionType.Shared
            };
            var asyncDatabase = new LiteDatabaseAsync(cs);
            asyncDatabase.UtcDate = true;
            serviceCollection.AddSingleton<ILiteDatabaseAsync>(asyncDatabase);
            return serviceCollection;
        }

        public static IServiceCollection AddInfrastructure(this IServiceCollection serviceCollection, bool useMemory = false)
        {
            if (useMemory)
            {
                serviceCollection.AddSingleton<IQueueService, MemoryQueueService>();
            }
            else
            {
                serviceCollection.AddSingleton<IQueueService, DbQueueService>();
            }

            return serviceCollection;
        }

        public static IServiceCollection AddCore(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IBrowserService, BrowserService>();
            return serviceCollection;
        }
        
        public static IServiceCollection WithBackend(this IServiceCollection serviceCollection)
        {
            if (OperatingSystem.IsWindows())
            {
                serviceCollection.AddSingleton<IBackend, Natives.Windows.Backend>();
            }
            else if (OperatingSystem.IsMacOS())
            {
                serviceCollection.AddSingleton<IBackend, Natives.MacOS.Backend>();
            }
            else if (OperatingSystem.IsLinux())
            {
                serviceCollection.AddSingleton<IBackend, Natives.Linux.Backend>();
            }

            return serviceCollection;
        }
    }
}
