using System.Runtime.InteropServices;
using System.Runtime.Versioning;

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
    internal static extern int RtlGetVersion(ref RTL_OSVERSIONINFOEX lpVersionInformation);
  }
}
