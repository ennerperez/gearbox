using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Gearbox.Core.Natives.Windows.Interop
{
    [SupportedOSPlatform("windows")]
    internal static class User32
    {
        [DllImport("user32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
        internal static extern nint GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
        internal static extern int GetWindowText(nint hWnd, string text, int count);

        [DllImport("user32.dll", SetLastError = true, ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
        internal static extern IntPtr CopyImage(HandleRef hImage, int uType, int cxDesired, int cyDesired, int fuFlags);
    }
}
