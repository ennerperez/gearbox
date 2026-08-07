using System.Diagnostics;
using System.Runtime.Versioning;

// ReSharper disable InconsistentNaming

namespace Gearbox.Core.Natives.MacOS.Interop
{
    [SupportedOSPlatform("macOS")]
    internal static class Xdo
    {
        public static string GetActiveWindowName()
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "/usr/bin/osascript",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                ArgumentList =
                {
                    "-e",
                    "tell application \"System Events\" to tell (first application process whose frontmost is true) to if exists (front window) then get name of front window else get name"
                }
            });
            var result = process?.StandardOutput.ReadToEnd().Trim();
            return result ?? string.Empty;
        }
    }
}
