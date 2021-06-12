using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using libuv2k.Native;

namespace libuv2k
{
    sealed class WriteRequest : ScheduleRequest
    {
        static readonly int BufferSize = Marshal.SizeOf<uv_buf_t>();

        internal readonly RequestContext requestContext;
        internal GCHandle handle;
        internal IntPtr handleAddr;

        IntPtr bufs;
        internal GCHandle pin;

        internal unsafe uv_buf_t* Bufs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (uv_buf_t*)bufs;
        }

        // Prepare() gets an ArraySegment, but we can't assume that it persists
        // until the completed callback. so we need to copy into an internal
        // buffer.
        // we use MaxMessageSize + 4 bytes header for every WriteRequest.
        // this way we avoid allocations since WriteRequest itself is pooled!
        internal byte[] data = new byte[TcpStream.MaxMessageWithHeaderSize];

        // UV_WRITE is for TCP write. UDP would require a different request type.
        internal WriteRequest() : base(uv_req_type.UV_WRITE)
        {
            requestContext = new RequestContext(uv_req_type.UV_WRITE, BufferSize, this);

            // pin request context
            IntPtr requestContextHandleAddr = requestContext.Handle;
            bufs = requestContextHandleAddr + requestContext.HandleSize;
            pin = GCHandle.Alloc(requestContextHandleAddr, GCHandleType.Pinned);

            // pin the data array once and keep it pinned until the end.
            // no need to unpin before returning to pool and pin again in
            // Prepare like we did originally.
            // => data is always same size
            // => pinning only once is faster
            handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            handleAddr = handle.AddrOfPinnedObject();
        }

        internal override IntPtr InternalHandle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => requestContext.Handle;
        }

        internal void Prepare(ArraySegment<byte> segment)
        {
            if (!requestContext.IsValid)
            {
                throw new ObjectDisposedException("WriteRequest status is invalid.");
            }

            // we can't assume that the ArraySegment that we passed in Send()
            // will persist until OnCompleted (in Mirror/DOTSNET it won't).
            // so we need to copy it to our internal data buffer.
            if (segment.Count > data.Length)
            {
                throw new ArgumentException("Segment.Count=" + segment.Count + " is too big for fixed internal buffer size=" + data.Length);
            }
            Buffer.BlockCopy(segment.Array, segment.Offset, data, 0, segment.Count);

            // 'data' was pinned in constructor already, no need to pin it again
            // here because the capacity did not change. simply init the libuv
            // memory.
            uv_buf_t.InitMemory(bufs, handleAddr, segment.Count);
        }

        internal void Free()
        {
            // release pinned request context
            if (pin.IsAllocated)
            {
                pin.Free();
            }

            // release pinned data array
            if (handle.IsAllocated)
            {
                handle.Free();
                handleAddr = IntPtr.Zero;
            }

            bufs = IntPtr.Zero;
        }

        protected override void Close()
        {
            if (bufs != IntPtr.Zero)
            {
                Free();
            }
            requestContext.Dispose();
        }
    }
}
