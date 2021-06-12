using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace libuv2k.Native
{
    [StructLayout(LayoutKind.Sequential)]
    struct uv_buf_t
    {
        static readonly bool IsWindows = Platform.IsWindows;
        static readonly int Size = IntPtr.Size;
        /*
           Windows
           public int length;
           public IntPtr data;

           Unix
           public IntPtr data;
           public IntPtr length;
        */

        // ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
        readonly IntPtr first;
        readonly IntPtr second;
        // ReSharper restore PrivateFieldCanBeConvertedToLocalVariable

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void InitMemory(IntPtr buf, IntPtr memory, int length)
        {
            IntPtr len = (IntPtr)length;
            if (IsWindows)
            {
                *(IntPtr*)buf = len;
                *(IntPtr*)(buf + Size) = memory;
            }
            else
            {
                *(IntPtr*)buf = memory;
                *(IntPtr*)(buf + Size) = len;
            }
        }

        internal uv_buf_t(IntPtr memory, int length)
        {
            if (length >= 0)
            {
                if (IsWindows)
                {
                    first = (IntPtr)length;
                    second = memory;
                }
                else
                {
                    first = memory;
                    second = (IntPtr)length;
                }
            }
            else throw new ArgumentException($"Length {length} needs to be >=0");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct uv_stream_t
    {
        // handle fields
        public IntPtr data;
        public IntPtr loop;
        public uv_handle_type type;
        public IntPtr close_cb;

        // stream fields
        public IntPtr write_queue_size; // number of bytes queued for writing
        public IntPtr alloc_cb;
        public IntPtr read_cb;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void uv_alloc_cb(IntPtr handle, IntPtr suggested_size, out uv_buf_t buf);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void uv_read_cb(IntPtr handle, IntPtr nread, ref uv_buf_t buf);

    static partial class NativeMethods
    {
        internal static void StreamReadStart(IntPtr handle)
        {
            if (handle != IntPtr.Zero)
            {
                int result = uv_read_start(handle, TcpStream.AllocateCallback, TcpStream.ReadCallback);
                ThrowIfError(result);
            }
            else throw new ArgumentException("handle can't be null!");
        }

        internal static void StreamReadStop(IntPtr handle)
        {
            if (handle != IntPtr.Zero)
            {
                int result = uv_read_stop(handle);
                ThrowIfError(result);
            }
            else throw new ArgumentException("handle can't be null!");
        }

        // Write data to stream. Buffers are written in order.
        // Note: The memory pointed to by the buffers must remain valid until the callback gets called.
        internal static unsafe void WriteStream(IntPtr requestHandle, IntPtr streamHandle, uv_buf_t* bufs, int size, uv_watcher_cb write_callback)
        {
            if (requestHandle != IntPtr.Zero && streamHandle != IntPtr.Zero)
            {
                int result = uv_write(requestHandle, streamHandle, bufs, size, write_callback);
                ThrowIfError(result);
            }
            else throw new ArgumentException("requestHandle and streamHandle can't be null!");
        }

        internal static void StreamListen(IntPtr handle, int backlog)
        {
            if (handle != IntPtr.Zero && backlog > 0)
            {
                int result = uv_listen(handle, backlog, TcpStream.ConnectionCallback);
                ThrowIfError(result);
            }
            else throw new ArgumentException("handle and backlog can't be null!");
        }

        internal static void StreamAccept(IntPtr serverHandle, IntPtr clientHandle)
        {
            if (serverHandle != IntPtr.Zero && clientHandle != IntPtr.Zero)
            {
                int result = uv_accept(serverHandle, clientHandle);
                ThrowIfError(result);
            }
            else throw new ArgumentException("serverHandle and clientHandle can't be null!");
        }

        // If *value == 0, it will return the current send buffer size,
        // otherwise it will use *value to set the new send buffersize.
        // This function works for TCP, pipe and UDP handles on Unix and for TCP and UDP handles on Windows.
        internal static int SendBufferSize(IntPtr handle, int value)
        {
            if (handle != IntPtr.Zero && value >= 0)
            {
                IntPtr size = (IntPtr)value;
                int result = uv_send_buffer_size(handle, ref size);
                ThrowIfError(result);

                return size.ToInt32();
            }
            else throw new ArgumentException($"handle {handle} can't be null and value {value} needs to be >= 0!");
        }

        // If *value == 0, it will return the current receive buffer size,
        // otherwise it will use *value to set the new receive buffer size.
        // This function works for TCP, pipe and UDP handles on Unix and for TCP and UDP handles on Windows.

        internal static int ReceiveBufferSize(IntPtr handle, int value)
        {
            if (handle != IntPtr.Zero && value >= 0)
            {
                IntPtr size = (IntPtr)value;
                int result = uv_recv_buffer_size(handle, ref size);
                ThrowIfError(result);

                return size.ToInt32();
            }
            else throw new ArgumentException($"handle {handle} can't be null and value {value} needs to be >= 0!");
        }

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_listen(IntPtr handle, int backlog, uv_watcher_cb connection_cb);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_accept(IntPtr server, IntPtr client);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_send_buffer_size(IntPtr handle, ref IntPtr value);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_recv_buffer_size(IntPtr handle, ref IntPtr value);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_read_start(IntPtr handle, uv_alloc_cb alloc_cb, uv_read_cb read_cb);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_read_stop(IntPtr handle);

        // Same as uv_write(), but won’t queue a write request if it can’t be completed immediately.
        // => won't need that for TCP games.
        //    we always need to write and queue if needed.
        //[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        //static extern int uv_try_write(IntPtr handle, uv_buf_t[] bufs, int bufcnt);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern unsafe int uv_write(IntPtr req, IntPtr handle, uv_buf_t* bufs, int nbufs, uv_watcher_cb cb);

        // we don't need to send handles. that's advanced libuv magic.
        //[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        //static extern unsafe int uv_write2(IntPtr req, IntPtr handle, uv_buf_t* bufs, int nbufs, IntPtr sendHandle, uv_watcher_cb cb);
    }
}
