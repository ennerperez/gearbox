using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace

namespace System.Runtime
{
    [ExcludeFromCodeCoverage]
    public static class OperatingSystemExtensions
    {
        public static string GetName()
        {
            if (OperatingSystem.IsWindows())
            {
                return "Windows";
            }
            else if (OperatingSystem.IsLinux())
            {
                return "Linux";
            }
            else if (OperatingSystem.IsMacOS())
            {
                return "MacOS";
            }
            else if (OperatingSystem.IsAndroid())
            {
                return "Android";
            }
            else if (OperatingSystem.IsIOS())
            {
                return "iOS";
            }
            else if (OperatingSystem.IsFreeBSD())
            {
                return "FreeBSD";
            }
            else if (OperatingSystem.IsBrowser())
            {
                return "Browser";
            }

            return string.Empty;
        }

        public static string GetDataDir()
        {
            string dataDir;
            var osAppDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrEmpty(osAppDataDir))
            {
                dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), $".{AssemblyMetadata.Product?.ToLower(Globalization.CultureInfo.CurrentCulture)}");
            }
            else
            {
                dataDir = Path.Combine(osAppDataDir, AssemblyMetadata.Product ?? string.Empty);
            }

            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            return dataDir;
        }
    }

    [ExcludeFromCodeCoverage]
    public static class ExceptionHandler
    {
        public static event UnhandledExceptionEventHandler UnhandledException;

        static ExceptionHandler()
        {
            // This is the normal event expected, and should still be used.
            // It will fire for exceptions from iOS and Mac Catalyst,
            // and for exceptions on background threads from WinUI 3.

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                UnhandledException?.Invoke(sender, args);
            };

            // Events fired by the TaskScheduler. That is calls like Task.Run(...)
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                if (sender == null)
                {
                    return;
                }

                args.SetObserved();
                UnhandledException?.Invoke(sender, new UnhandledExceptionEventArgs(args.Exception, false));
            };

            AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
            {
                if (sender != null)
                {
                    UnhandledException?.Invoke(sender, new UnhandledExceptionEventArgs(args.Exception, false));
                }
            };
        }
    }

    [ExcludeFromCodeCoverage]
    public static class Environments
    {
        /// <summary>
        /// Specifies the Development environment.
        /// </summary>
        /// <remarks>The development environment can enable features that shouldn't be exposed in production. Because of the performance cost, scope validation and dependency validation only happens in development.</remarks>
        public static readonly string Development = "Development";
        /// <summary>
        /// Specifies the Staging environment.
        /// </summary>
        /// <remarks>The staging environment can be used to validate app changes before changing the environment to production.</remarks>
        public static readonly string Staging = "Staging";
        /// <summary>
        /// Specifies the Production environment.
        /// </summary>
        /// <remarks>The production environment should be configured to maximize security, performance, and application robustness.</remarks>
        public static readonly string Production = "Production";
    }
}
