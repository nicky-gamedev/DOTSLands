using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace libuv2k.Native
{
    enum uv_handle_type
    {
        UV_UNKNOWN_HANDLE = 0,
        UV_ASYNC,
        UV_CHECK,
        UV_FS_EVENT,
        UV_FS_POLL,
        UV_HANDLE,
        UV_IDLE,
        UV_NAMED_PIPE,
        UV_POLL,
        UV_PREPARE,
        UV_PROCESS,
        UV_STREAM,
        UV_TCP,
        UV_TIMER,
        UV_TTY,
        UV_UDP,
        UV_SIGNAL,
        UV_FILE,
        UV_HANDLE_TYPE_MAX
    }

    [StructLayout(LayoutKind.Sequential)]
    struct uv_handle_t
    {
        public IntPtr data;
        public IntPtr loop;
        public uv_handle_type type;
        public IntPtr close_cb;
    }

    /// <summary>
    /// https://github.com/aspnet/KestrelHttpServer/blob/dev/src/Microsoft.AspNetCore.Server.Kestrel/Internal/Networking/SockAddr.cs
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    struct sockaddr
    {
        // this type represents native memory occupied by sockaddr struct
        // https://msdn.microsoft.com/en-us/library/windows/desktop/ms740496(v=vs.85).aspx
        // although the c/c++ header defines it as a 2-byte short followed by a 14-byte array,
        // the simplest way to reserve the same size in c# is with four nameless long values
        public long field0;
        public long field1;
        public long field2;
        public long field3;

        // ReSharper disable once UnusedParameter.Local
        internal sockaddr(long ignored) // unused, but keep it (see above)
        {
            field0 = field1 = field2 = field3 = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe IPEndPoint GetIPEndPoint()
        {
            // The bytes are represented in network byte order.
            //
            // Example 1: [2001:4898:e0:391:b9ef:1124:9d3e:a354]:39179
            //
            // 0000 0000 0b99 0017  => The third and fourth bytes 990B is the actual port
            // 9103 e000 9848 0120  => IPv6 address is represented in the 128bit field1 and field2.
            // 54a3 3e9d 2411 efb9     Read these two 64-bit long from right to left byte by byte.
            // 0000 0000 0000 0000
            //
            // Example 2: 10.135.34.141:39178 when adopt dual-stack sockets, IPv4 is mapped to IPv6
            //
            // 0000 0000 0a99 0017  => The port representation are the same
            // 0000 0000 0000 0000
            // 8d22 870a ffff 0000  => IPv4 occupies the last 32 bit: 0A.87.22.8d is the actual address.
            // 0000 0000 0000 0000
            //
            // Example 3: 10.135.34.141:12804, not dual-stack sockets
            //
            // 8d22 870a fd31 0002  => sa_family == AF_INET (02)
            // 0000 0000 0000 0000
            // 0000 0000 0000 0000
            // 0000 0000 0000 0000
            //
            // Example 4: 127.0.0.1:52798, on a Mac OS
            //
            // 0100 007F 3ECE 0210  => sa_family == AF_INET (02) Note that struct sockaddr on mac use
            // 0000 0000 0000 0000     the second unint8 field for sa family type
            // 0000 0000 0000 0000     http://www.opensource.apple.com/source/xnu/xnu-1456.1.26/bsd/sys/socket.h
            // 0000 0000 0000 0000
            //
            // Reference:
            //  - Windows: https://msdn.microsoft.com/en-us/library/windows/desktop/ms740506(v=vs.85).aspx
            //  - Linux: https://github.com/torvalds/linux/blob/6a13feb9c82803e2b815eca72fa7a9f5561d7861/include/linux/socket.h
            //  - Apple: http://www.opensource.apple.com/source/xnu/xnu-1456.1.26/bsd/sys/socket.h

            // Quick calculate the port by mask the field and locate the byte 3 and byte 4
            // and then shift them to correct place to form a int.
            int port = ((int)(field0 & 0x00FF0000) >> 8) | (int)((field0 & 0xFF000000) >> 24);

            int family = (int)field0;
            if (Platform.IsMacOS)
            {
                // see explaination in example 4
                family = family >> 8;
            }
            family = family & 0xFF;

            if (family == 2)
            {
                // AF_INET => IPv4
                return new IPEndPoint(new IPAddress((field0 >> 32) & 0xFFFFFFFF), port);
            }
            else if (IsIPv4MappedToIPv6())
            {
                long ipv4bits = (field2 >> 32) & 0x00000000FFFFFFFF;
                return new IPEndPoint(new IPAddress(ipv4bits), port);
            }
            else
            {
                // otherwise IPv6
                byte[] bytes = new byte[16];
                fixed (byte* b = bytes)
                {
                    *((long*)b) = field1;
                    *((long*)(b + 8)) = field2;
                }

                return new IPEndPoint(new IPAddress(bytes), port);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsIPv4MappedToIPv6()
        {
            // If the IPAddress is an IPv4 mapped to IPv6, return the IPv4 representation instead.
            // For example [::FFFF:127.0.0.1] will be transform to IPAddress of 127.0.0.1
            if (field1 != 0)
            {
                return false;
            }

            return (field2 & 0xFFFFFFFF) == 0xFFFF0000;
        }
    }

    static partial class NativeMethods
    {
        internal static HandleContext Initialize(IntPtr loopHandle, uv_handle_type handleType, TcpStream target)
        {
            if (loopHandle != IntPtr.Zero && target != null)
            {
                switch (handleType)
                {
                    case uv_handle_type.UV_TCP:
                        return new HandleContext(handleType, InitializeTcp, loopHandle, target);
                    default:
                        throw new NotSupportedException($"Handle type to initialize {handleType} not supported");
                }
            }
            else throw new ArgumentException($"loopHandle {loopHandle} and target {target} can't be null!");
        }

        static int InitializeTcp(IntPtr loopHandle, IntPtr handle)
        {
            if (loopHandle != IntPtr.Zero && handle != IntPtr.Zero)
            {
                return uv_tcp_init(loopHandle, handle);
            }
            else throw new ArgumentException("loopHandle and handle can't be null!");
        }

        static readonly int[] HandleSizeTable;
        static readonly int[] RequestSizeTable;

        static NativeMethods()
        {
            HandleSizeTable = new []
            {
                uv_handle_size(uv_handle_type.UV_ASYNC).ToInt32(),
                uv_handle_size(uv_handle_type.UV_CHECK).ToInt32(),
                uv_handle_size(uv_handle_type.UV_FS_EVENT).ToInt32(),
                uv_handle_size(uv_handle_type.UV_FS_POLL).ToInt32(),
                uv_handle_size(uv_handle_type.UV_HANDLE).ToInt32(),
                uv_handle_size(uv_handle_type.UV_IDLE).ToInt32(),
                uv_handle_size(uv_handle_type.UV_NAMED_PIPE).ToInt32(),
                uv_handle_size(uv_handle_type.UV_POLL).ToInt32(),
                uv_handle_size(uv_handle_type.UV_PREPARE).ToInt32(),
                uv_handle_size(uv_handle_type.UV_PROCESS).ToInt32(),
                uv_handle_size(uv_handle_type.UV_STREAM).ToInt32(),
                uv_handle_size(uv_handle_type.UV_TCP).ToInt32(),
                uv_handle_size(uv_handle_type.UV_TIMER).ToInt32(),
                uv_handle_size(uv_handle_type.UV_TTY).ToInt32(),
                uv_handle_size(uv_handle_type.UV_UDP).ToInt32(),
                uv_handle_size(uv_handle_type.UV_SIGNAL).ToInt32(),
                uv_handle_size(uv_handle_type.UV_FILE).ToInt32(),
            };

            RequestSizeTable = new []
            {
                uv_req_size(uv_req_type.UV_REQ).ToInt32(),
                uv_req_size(uv_req_type.UV_CONNECT).ToInt32(),
                uv_req_size(uv_req_type.UV_WRITE).ToInt32(),
                uv_req_size(uv_req_type.UV_SHUTDOWN).ToInt32(),
                uv_req_size(uv_req_type.UV_UDP_SEND).ToInt32(),
                uv_req_size(uv_req_type.UV_FS).ToInt32(),
                uv_req_size(uv_req_type.UV_WORK).ToInt32(),
                uv_req_size(uv_req_type.UV_GETADDRINFO).ToInt32(),
                uv_req_size(uv_req_type.UV_GETNAMEINFO).ToInt32()
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetSize(uv_handle_type handleType) =>
            HandleSizeTable[unchecked((int)handleType - 1)];

        internal static void TcpSetNoDelay(IntPtr handle, bool value)
        {
            if (handle != IntPtr.Zero)
            {
                int result = uv_tcp_nodelay(handle, value ? 1 : 0);
                ThrowIfError(result);
            }
            else throw new ArgumentException("handle can't be null!");
        }

        internal static void TcpSetKeepAlive(IntPtr handle, bool value, int delay)
        {
            if (handle != IntPtr.Zero && delay > 0)
            {
                int result = uv_tcp_keepalive(handle, value ? 1 : 0, delay);
                ThrowIfError(result);
            }
            else throw new ArgumentException("handle and delay can't be null!");
        }

        internal static void TcpSimultaneousAccepts(IntPtr handle, bool value)
        {
            if (handle != IntPtr.Zero)
            {
                int result = uv_tcp_simultaneous_accepts(handle, value ? 1 : 0);
                ThrowIfError(result);
            }
            else throw new ArgumentException("handle can't be null!");
        }

        internal static void TcpBind(IntPtr handle, IPEndPoint endPoint, bool dualStack /* Both IPv4 & IPv6 */)
        {
            if (handle != IntPtr.Zero && endPoint != null)
            {
                GetSocketAddress(endPoint, out sockaddr addr);

                int result = uv_tcp_bind(handle, ref addr, (uint)(dualStack ? 1 : 0));
                ThrowIfError(result);
            }
            else throw new ArgumentException("handle and endPoint can't be null!");
        }

        internal static void TcpConnect(IntPtr requestHandle, IntPtr handle, IPEndPoint endPoint)
        {
            if (requestHandle != IntPtr.Zero && handle != IntPtr.Zero && endPoint != null)
            {
                GetSocketAddress(endPoint, out sockaddr addr);

                int result = uv_tcp_connect(requestHandle, handle, ref addr, WatcherRequest.WatcherCallback);
                ThrowIfError(result);
            }
            else throw new ArgumentException("requestHandle, handle, endPoint can't be null!");
        }

        internal static IPEndPoint TcpGetSocketName(IntPtr handle)
        {
            if (handle != IntPtr.Zero)
            {
                int namelen = Marshal.SizeOf<sockaddr>();
                uv_tcp_getsockname(handle, out sockaddr sockaddr, ref namelen);

                return sockaddr.GetIPEndPoint();
            }
            else throw new ArgumentException("handle can't be null!");
        }

        internal static IPEndPoint TcpGetPeerName(IntPtr handle)
        {
            if (handle != IntPtr.Zero)
            {
                int namelen = Marshal.SizeOf<sockaddr>();
                int result = uv_tcp_getpeername(handle, out sockaddr sockaddr, ref namelen);
                ThrowIfError(result);

                return sockaddr.GetIPEndPoint();
            }
            else throw new ArgumentException("handle can't be null!");
        }

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_tcp_connect(IntPtr req, IntPtr handle, ref sockaddr sockaddr, uv_watcher_cb connect_cb);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_tcp_init(IntPtr loopHandle, IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_tcp_bind(IntPtr handle, ref sockaddr sockaddr, uint flags);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_tcp_getsockname(IntPtr handle, out sockaddr sockaddr, ref int namelen);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_tcp_getpeername(IntPtr handle, out sockaddr name, ref int namelen);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_tcp_nodelay(IntPtr handle, int enable);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_tcp_keepalive(IntPtr handle, int enable, int delay);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_tcp_simultaneous_accepts(IntPtr handle, int enable);

        internal static void GetSocketAddress(IPEndPoint endPoint, out sockaddr addr)
        {
            if (endPoint != null)
            {
                string ip = endPoint.Address.ToString();
                int result;
                switch (endPoint.AddressFamily)
                {
                    case AddressFamily.InterNetwork:
                        result = uv_ip4_addr(ip, endPoint.Port, out addr);
                        break;
                    case AddressFamily.InterNetworkV6:
                        result = uv_ip6_addr(ip, endPoint.Port, out addr);
                        break;
                    default:
                        throw new NotSupportedException($"End point {endPoint} is not supported, expecting InterNetwork/InterNetworkV6.");
                }
                ThrowIfError(result);
            }
            else throw new ArgumentException("endPoint can't be null!");
        }

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_ip4_addr(string ip, int port, out sockaddr address);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_ip6_addr(string ip, int port, out sockaddr address);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr uv_handle_size(uv_handle_type handleType);
    }
}
