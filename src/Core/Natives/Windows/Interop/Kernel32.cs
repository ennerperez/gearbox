using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Gearbox.Core.Natives.Windows.Interop
{
    [SupportedOSPlatform("windows")]
    internal static class Kernel32
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
        public static extern nint GetModuleHandle(string lpModuleName);

        // ReSharper disable once InconsistentNaming
        private const uint ATTACH_PARENT_PROCESS = 0x0ffffffff;

        [DllImport("kernel32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
        private static extern bool AttachConsole(uint dwProcessId);

        public static void AttachToParentConsole() => AttachConsole(ATTACH_PARENT_PROCESS);
    }
}
