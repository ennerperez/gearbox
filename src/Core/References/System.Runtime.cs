using System.IO;
using System.Threading.Tasks;
using Gearbox.Core;

// ReSharper disable once CheckNamespace

namespace System.Runtime
{
  public static class OperatingSystemExtensions
  {
    public static string GetName()
    {
      if (OperatingSystem.IsWindows()) return "Windows";
      else if (OperatingSystem.IsLinux()) return "Linux";
      else if (OperatingSystem.IsMacOS()) return "MacOS";
      else if (OperatingSystem.IsAndroid()) return "Android";
      else if (OperatingSystem.IsIOS()) return "iOS";
      else if (OperatingSystem.IsFreeBSD()) return "FreeBSD";
      else if (OperatingSystem.IsBrowser()) return "Browser";
      return string.Empty;
    }

    public static string GetDataDir()
    {
      string dataDir;
      var osAppDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
      if (string.IsNullOrEmpty(osAppDataDir))
      {
        dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), $".{Metadata.Name?.ToLower()}");
      }
      else
      {
        dataDir = Path.Combine(osAppDataDir, Metadata.Name ?? string.Empty);
      }

      if (!Directory.Exists(dataDir))
      {
        Directory.CreateDirectory(dataDir);
      }
      return dataDir;
    }
  }

  public static class Exceptions
  {
    public static event UnhandledExceptionEventHandler? UnhandledException;
    static Exceptions()
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
        if (sender != null)
        {
          args.SetObserved();
          UnhandledException?.Invoke(sender, new UnhandledExceptionEventArgs(args.Exception, false));
        }
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
}
