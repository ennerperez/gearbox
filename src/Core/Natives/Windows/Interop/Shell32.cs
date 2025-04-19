using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Gearbox.Core.Natives.Windows.Interop
{
    [SupportedOSPlatform("windows")]
    internal static class Shell32
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        internal static extern unsafe IntPtr ExtractAssociatedIcon(HandleRef hInst, char* iconPath, ref int index);
    }
}
