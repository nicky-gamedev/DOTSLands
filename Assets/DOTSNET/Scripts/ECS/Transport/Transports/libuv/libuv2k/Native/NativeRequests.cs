using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace libuv2k.Native
{
    enum uv_req_type
    {
        UV_UNKNOWN_REQ = 0,
        UV_REQ,
        UV_CONNECT,
        UV_WRITE,
        UV_SHUTDOWN,
        UV_UDP_SEND,
        UV_FS,
        UV_WORK,
        UV_GETADDRINFO,
        UV_GETNAMEINFO,
        UV_REQ_TYPE_PRIVATE,
        UV_REQ_TYPE_MAX
    }

    [StructLayout(LayoutKind.Sequential)]
    struct uv_req_t
    {
        public IntPtr data;
        public uv_req_type type;
    }

    static partial class NativeMethods
    {
        internal static void Shutdown(IntPtr requestHandle, IntPtr streamHandle)
        {
            if (requestHandle  != IntPtr.Zero && streamHandle != IntPtr.Zero)
            {
                int result = uv_shutdown(requestHandle, streamHandle, WatcherRequest.WatcherCallback);
                ThrowIfError(result);
            }
            else throw new ArgumentException("requestHandle and streamHandle can't be null!");
        }

        internal static bool Cancel(IntPtr handle) =>  handle != IntPtr.Zero && uv_cancel(handle) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetSize(uv_req_type requestType) =>  RequestSizeTable[unchecked((int)requestType - 1)];

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_cancel(IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_shutdown(IntPtr requestHandle, IntPtr streamHandle, uv_watcher_cb callback);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr uv_req_size(uv_req_type reqType);
    }
}
