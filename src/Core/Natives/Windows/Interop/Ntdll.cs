using System.Runtime.InteropServices;
using System.Runtime.Versioning;

// ReSharper disable InconsistentNaming

namespace Gearbox.Core.Natives.Windows.Interop
{
    [SupportedOSPlatform("windows")]
    internal static class Ntdll
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct RTL_OSVERSIONINFOEX
        {
            internal uint dwOSVersionInfoSize;
            internal uint dwMajorVersion;
            internal uint dwMinorVersion;
            internal uint dwBuildNumber;
            internal uint dwPlatformId;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string szCSDVersion;
        }

        [DllImport("ntdll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
        internal static extern int RtlGetVersion(ref RTL_OSVERSIONINFOEX lpVersionInformation);
    }
}
