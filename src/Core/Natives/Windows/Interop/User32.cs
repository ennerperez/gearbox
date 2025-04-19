using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace Gearbox.Core.Natives.Windows.Interop
{
    [SupportedOSPlatform("windows")]
    internal static class User32
    {
        [DllImport("user32.dll")]
        internal static extern nint GetForegroundWindow();

        [DllImport("user32.dll")]
        internal static extern int GetWindowText(nint hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", SetLastError = true, ExactSpelling = true)]
        internal static extern IntPtr CopyImage(HandleRef hImage, int uType, int cxDesired, int cyDesired, int fuFlags);
    }
}
