using System.Runtime.InteropServices;

namespace libuv2k.Native
{
    static class Platform
    {
        public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static readonly bool IsMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static readonly bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }
}
