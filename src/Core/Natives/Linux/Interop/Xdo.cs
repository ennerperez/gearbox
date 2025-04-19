using System.Diagnostics;
using System.Runtime.Versioning;

// ReSharper disable InconsistentNaming

namespace Gearbox.Core.Natives.Linux.Interop
{
    [SupportedOSPlatform("linux")]
    internal static class Xdo
    {
        public static string GetActiveWindowName()
        {
            using var process = Process.Start(new ProcessStartInfo() { FileName = "xdotool", Arguments = "getactivewindow getwindowname", RedirectStandardOutput = true });
            var result = process?.StandardOutput.ReadToEnd().Trim();
            return result ?? string.Empty;
        }
    }
}
