using System.Diagnostics;
using System.Runtime.Versioning;
// ReSharper disable InconsistentNaming

namespace Gearbox.Core.Natives.Linux.Interop
{
  [SupportedOSPlatform("linux")]
  internal static class Xdg
  {
    internal const string GNOME_DESKTOP = "GNOME";
    internal const string KDE_DESKTOP = "KDE";
    public static string CurrentDesktop()
    {
      return System.Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? string.Empty;
    }

    public static void Open(string path)
    {
      using var process = Process.Start(new ProcessStartInfo() { FileName = "xdg-open", Arguments = $"{path}", RedirectStandardOutput = true });
    }

    internal const string DEFAULT_WEB_BROWSER = "default-web-browser";

    public static string GetSetting(string key)
    {
      using var process = Process.Start(new ProcessStartInfo() { FileName = "xdg-settings", Arguments = $"get {key}", RedirectStandardOutput = true });
      var result = process?.StandardOutput.ReadToEnd().Trim();
      return result ?? string.Empty;
    }
    public static string SetSetting(string key, string value)
    {
      using var process = Process.Start(new ProcessStartInfo() { FileName = "xdg-settings", Arguments = $"set {key} {value}", RedirectStandardOutput = true });
      var result = process?.StandardOutput.ReadToEnd().Trim();
      return result ?? string.Empty;
    }

    public static void Mime(string app, string mime)
    {
      using var process = Process.Start(new ProcessStartInfo() { FileName = "xdg-mime", Arguments = $"default {app} {mime}", RedirectStandardOutput = true });
    }
    public static string UserDir()
    {
      using var process = Process.Start(new ProcessStartInfo() { FileName = "xdg-user-dir", RedirectStandardOutput = true });
      var result = process?.StandardOutput.ReadToEnd().Trim();;
      return result ?? string.Empty;
    }
  }
}
