using AOT;
using System;
using System.Net;
using System.Runtime.InteropServices;
using libuv2k.Native;

namespace libuv2k
{
    public sealed class TcpStream : IDisposable
    {
        internal const int DefaultBacklog = 128;

        // IMPORTANT: converting our C# functions to uv_alloc_cb etc. allocates!
        // let's only do this once here and then use the converted ones everywhere.
        internal static readonly uv_alloc_cb AllocateCallback = OnAllocateCallback;
        internal static readonly uv_read_cb ReadCallback = OnReadCallback;
        internal static readonly uv_watcher_cb ConnectionCallback = OnConnectionCallback;
        internal static readonly uv_watcher_cb WriteCallback = OnWriteCallback;
        Action<ArraySegment<byte>> FramingCallback;

        readonly HandleContext handle;
        internal readonly uv_handle_type HandleType;

        // server connection handler
        Action<TcpStream, Exception> onClientConnect;
        Action<TcpStream, Exception> onServerConnect;
        Action<TcpStream> closeCallback;
        // onMessage passes an ArraySegment to avoid byte copying.
        // the segment is valid only until onData returns.
        // we use framing, so the segment is the actual message, not just random
        // stream data.
        public Action<TcpStream, ArraySegment<byte>> onMessage;
        public Action<TcpStream, Exception> onError;
        // onCompleted is called before we close the stream.
        // afterwards we close the stream, and onClosed will be called.
        public Action<TcpStream> onCompleted;
        // onClosed is called after we closed the stream
        public Action<TcpStream> onClosed;
        // onShutdown is called after we shutdown the stream
        public Action<TcpStream, Exception> onShutdown;

        // pinned readBuffer for pending reads
        // libuv Allocate+Read callbacks always go in pairs:
        //   https://github.com/libuv/libuv/issues/1085
        //   https://libuv.narkive.com/cn1gwvvA/passing-a-small-buffer-to-alloc-cb-results-in-multiple-calls-to-alloc-cb
        // So we allocate the buffer only once, and then reuse it.
        // we also pin it so that GC doesn't move it while libuv is reading.
        byte[] readBuffer;
        internal GCHandle readBufferPin;
        uv_buf_t readBufferStruct; // for libuv native C

        // libuv handle
        public bool IsActive => handle.IsActive;
        public bool IsClosing => handle.IsClosing;
        public bool IsValid => handle.IsValid;
        public object UserToken { get; set; }
        public IntPtr InternalHandle => handle.Handle;

        // framing buffer.
        // read buffer is of ReceiveBufferSize (~408KB on mac).
        // framing buffer is of MaxMessageWithHeaderSize to build messages into.
        byte[] framingBuffer = new byte[MaxMessageWithHeaderSize];
        int framingBufferPosition = 0;

        // payload buffer for message sending.
        // we need a buffer to construct <length:4, data:length> packets.
        byte[] payloadBuffer = new byte[MaxMessageWithHeaderSize];

        // libuv uses good internal send/recv buffer sizes which we should not
        // change. we use the internal recv buffer size for our receive buffer.
        // for WriteRequest we need MaxMessageSize. using SendBufferSize would
        // not make sense since WriteRequests always write up to MaxMessageSize.
        // this way we don't need to worry about runtime send buffer size
        // changes either (Linux does a lot of buffer size magic).
        //
        // we will also need MaxMessageSize for framing.
        public const int MaxMessageSize = 64 * 1024;
        public const int MaxMessageWithHeaderSize = MaxMessageSize + 4;

        // we need a watcher request for ongoing connects to get notified about
        // the result
        WatcherRequest connectRequest;

        // we need a watcher request for shutting down the stream to get
        // notified about the result
#pragma warning disable 649
        WatcherRequest shutdownRequest;
#pragma warning restore 649

        // one static WriteRequest pool to avoid allocations
        internal static readonly Pool<WriteRequest> WriteRequestPool =
            new Pool<WriteRequest>(
                () => new WriteRequest(),
                (obj) => obj.Dispose()
            );

        // constructor /////////////////////////////////////////////////////////
        public TcpStream(Loop loop)
        {
            // make sure loop can be used
            loop.Validate();

            // converting OnFramingCallback to Action allocates.
            // lets do it only once, instead of every ReadCallback.
            FramingCallback = OnFramingCallback;

            HandleContext initialHandle = NativeMethods.Initialize(loop.Handle, uv_handle_type.UV_TCP, this);
            if (initialHandle != null)
            {
                handle = initialHandle;
                HandleType = uv_handle_type.UV_TCP;

                // note: setting send/recv buffer sizes gives EBADF when trying it
                // here. instead we call ConfigureSendReceiveBufferSize after
                // successful connects.
            }
            else throw new ArgumentException($"{nameof(TcpStream)} Initialize failed for handle: {loop.Handle}");
        }

        // creates a new TcpStream for a connecting client
        internal unsafe TcpStream NewStream()
        {
            IntPtr loopHandle = ((uv_stream_t*)InternalHandle)->loop;
            Loop loop = HandleContext.GetTarget<Loop>(loopHandle);

            TcpStream client = new TcpStream(loop);
            NativeMethods.StreamAccept(InternalHandle, client.InternalHandle);
            client.ReadStart();

            //Logger.Log($"{HandleType} {InternalHandle} client {client.InternalHandle} accepted.");

            return client;
        }

        // pinned read buffer //////////////////////////////////////////////////
        internal uv_buf_t PinReadBuffer()
        {
            // pin it so GC doesn't dispose or move it while libuv is using
            // it.
            //
            // the assert is extremely slow, and it allocates.
            //Debug.Assert(!this.pin.IsAllocated);
            readBufferPin = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            IntPtr arrayHandle = readBufferPin.AddrOfPinnedObject();
            return new uv_buf_t(arrayHandle, readBuffer.Length);
        }

        internal void UnpinReadBuffer()
        {
            if (readBufferPin.IsAllocated)
            {
                readBufferPin.Free();
            }
        }

        // handle //////////////////////////////////////////////////////////////
        internal void Validate() => handle.Validate();

        // tcp configuration ///////////////////////////////////////////////////
        public void NoDelay(bool value)
        {
            Validate();
            NativeMethods.TcpSetNoDelay(InternalHandle, value);
        }

        public void KeepAlive(bool value, int delay)
        {
            Validate();
            NativeMethods.TcpSetKeepAlive(InternalHandle, value, delay);
        }

        public void SimultaneousAccepts(bool value)
        {
            Validate();
            NativeMethods.TcpSimultaneousAccepts(InternalHandle, value);
        }

        // find out libuv's send buffer size
        public int GetSendBufferSize()
        {
            Validate();
            return NativeMethods.SendBufferSize(InternalHandle, 0);
        }

        // find out libuv's recv buffer size
        public int GetReceiveBufferSize()
        {
            Validate();
            return NativeMethods.ReceiveBufferSize(InternalHandle, 0);
        }

        // buffers /////////////////////////////////////////////////////////////
        // create read buffer and pin it so GC doesn't move it while libuv uses
        // it.
        internal void CreateAndPinReadBuffer(int size)
        {
            readBuffer = new byte[size];
            readBufferStruct = PinReadBuffer();
        }

        // libuv uses 408KB recv buffer and 146KB send buffer on OSX by default.
        // that's plenty of space for batching, and there is no easy way to find
        // good send/recv buffer values across all platforms (especially linux).
        // let libuv handle it, don't allow modifying them.
        // (which makes allocation free byte[] pooling easier too)
        //
        //   internal int SetSendBufferSize(int value)
        //   {
        //       Validate();
        //       return NativeMethods.SendBufferSize(InternalHandle, value);
        //   }
        //   internal int SetReceiveBufferSize(int value)
        //   {
        //       Validate();
        //       return NativeMethods.ReceiveBufferSize(InternalHandle, value);
        //   }

        // read buffer should be initialized to libuv's buffer sizes as soon as
        // we can access them safely without EBADF after connecting.
        // we also log both sizes so the user knows what to expect.
        internal void InitializeInternalBuffers()
        {
            int sendSize = GetSendBufferSize();
            int recvSize = GetReceiveBufferSize();
            Log.Info($"libuv send buffer size = {sendSize}, recv buffer size = {recvSize}");

            // create read buffer and pin it so GC doesn't move it while libuv
            // uses it.
            if (readBuffer == null)
            {
                CreateAndPinReadBuffer(recvSize);
            }
            else Log.Error($"{nameof(InitializeInternalBuffers)} called twice. That should never happen.");
        }

        // endpoints ///////////////////////////////////////////////////////////
        public IPEndPoint GetLocalEndPoint()
        {
            Validate();
            return NativeMethods.TcpGetSocketName(InternalHandle);
        }

        public IPEndPoint GetPeerEndPoint()
        {
            Validate();
            return NativeMethods.TcpGetPeerName(InternalHandle);
        }

        // bind ////////////////////////////////////////////////////////////////
        public void Bind(IPEndPoint endPoint, bool dualStack = false)
        {
            if (endPoint != null)
            {
                Validate();
                NativeMethods.TcpBind(InternalHandle, endPoint, dualStack);
            }
            else throw new ArgumentException("EndPoint can't be null!");
        }

        // listen //////////////////////////////////////////////////////////////
        public void Listen(Action<TcpStream, Exception> onConnection, int backlog = DefaultBacklog)
        {
            if (onConnection != null && backlog > 0)
            {
                Validate();
                onServerConnect = onConnection;
                try
                {
                    NativeMethods.StreamListen(InternalHandle, backlog);
                    //Log.Info($"Stream {HandleType} {InternalHandle} listening, backlog = {backlog}");
                }
                catch
                {
                    Dispose();
                    throw;
                }
            }
            else throw new ArgumentException($"onServerConnect {onServerConnect} and backlog {backlog} can't be null!");
        }

        public void Listen(IPEndPoint localEndPoint, Action<TcpStream, Exception> onConnection, int backlog = DefaultBacklog, bool dualStack = false)
        {
            if (localEndPoint != null && onConnection != null)
            {
                Bind(localEndPoint, dualStack);
                Listen(onConnection, backlog);
            }
            else throw new ArgumentException($"onConnection {onConnection} and localEndPoint {localEndPoint} can't be null!");
        }

        // connect /////////////////////////////////////////////////////////////
        void ConnectRequestCallback(WatcherRequest request, Exception error)
        {
            try
            {
                if (error == null)
                {
                    // initialize internal buffer after we can access libuv
                    // send/recv buffer sizes (which is after connecting)
                    InitializeInternalBuffers();
                    ReadStart();
                }

                onClientConnect(this, error);
            }
            finally
            {
                connectRequest.Dispose();
                connectRequest = null;
            }
        }

        public void ConnectTo(IPEndPoint remoteEndPoint, Action<TcpStream, Exception> connectedAction, bool dualStack = false)
        {
            if (remoteEndPoint != null && connectedAction != null)
            {
                // let's only connect if a connect isn't in progress already.
                // this way we only need to keep track of one WatcherRequest.
                if (connectRequest == null)
                {
                    onClientConnect = connectedAction;

                    try
                    {
                        connectRequest = new WatcherRequest(
                            uv_req_type.UV_CONNECT,
                            ConnectRequestCallback,
                            h => NativeMethods.TcpConnect(h, InternalHandle, remoteEndPoint));
                    }
                    catch (Exception)
                    {
                        connectRequest?.Dispose();
                        connectRequest = null;
                        throw;
                    }
                }
                else
                {
                    Log.Warning("A connect is already in progress. Please wait for it to finish before connecting again.");
                }
            }
            else throw new ArgumentException($"remoteEndPoint {remoteEndPoint}, connectedAction {connectedAction} can't be null!");
        }

        public void ConnectTo(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, Action<TcpStream, Exception> connectedAction, bool dualStack = false)
        {
            if (localEndPoint != null && remoteEndPoint != null & connectedAction != null)
            {
                Bind(localEndPoint, dualStack);
                ConnectTo(remoteEndPoint, connectedAction);
            }
            else throw new ArgumentException($"localEndPoint {localEndPoint}, remoteEndPoint {remoteEndPoint}, connectedAction {connectedAction} can't be null!");
        }

        // write ///////////////////////////////////////////////////////////////
        // WriteCallback is called after writing.
        // we use it to return the WriteRequest to the pool in order to avoid
        // allocations.
        //
        // C->C# callbacks need to be static and have MonoPInvokeCallback for
        // IL2CPP builds. avoids "System.NotSupportedException: To marshal a
        // managed method please add an attribute named 'MonoPInvokeCallback'"
        [MonoPInvokeCallback(typeof(uv_watcher_cb))]
        static void OnWriteCallback(IntPtr handle, int status)
        {
            WriteRequest request = RequestContext.GetTarget<WriteRequest>(handle);
            WriteRequestPool.Return(request);
        }

        // WriteStream writes the final WriteRequest
        internal unsafe void WriteStream(WriteRequest request)
        {
            if (request != null)
            {
                Validate();
                try
                {
                    // WriteRequest could keep multiple buffers, but only keeps one
                    // so we always pass '1' as size
                    NativeMethods.WriteStream(request.InternalHandle, InternalHandle, request.Bufs, 1, WriteCallback);
                }
                catch (Exception exception)
                {
                    Log.Error($"{HandleType} Failed to write data {request}: {exception}");
                    throw;
                }
            }
            else throw new ArgumentException("Request can't be null!");
        }

        // segment data is copied internally and can be reused immediately.
        public void Send(ArraySegment<byte> segment)
        {
            // make sure we don't try to write anything larger than WriteRequest
            // internal buffer size, which is MaxMessageSize + 4 for header
            if (segment.Count <= MaxMessageSize)
            {
                // create <size, data> payload so we only call write once.
                if (Framing.Frame(payloadBuffer, segment))
                {
                    // queue write the payload
                    ArraySegment<byte> payload = new ArraySegment<byte>(payloadBuffer, 0, segment.Count + 4);
                    WriteRequest request = WriteRequestPool.Take();
                    try
                    {
                        // prepare request with our completion callback, and make
                        // sure that streamHandle is passed as first parameter.
                        request.Prepare(payload);
                        WriteStream(request);
                    }
                    catch (Exception exception)
                    {
                        Log.Error($"{HandleType} faulted: {exception}");
                        WriteRequestPool.Return(request);
                        throw;
                    }
                }
                else Log.Error($"Framing failed for message with size={segment.Count}. Make sure it's within MaxMessageSize={MaxMessageSize}");
            }
            else Log.Error($"Failed to send message of size {segment.Count} because it's larger than MaxMessageSize={MaxMessageSize}");
        }

        // read ////////////////////////////////////////////////////////////////
        internal void ReadStart()
        {
            Validate();
            NativeMethods.StreamReadStart(InternalHandle);
        }

        internal void ReadStop()
        {
            if (!IsValid)
            {
                return;
            }

            // This function is idempotent and may be safely called on a stopped stream.
            NativeMethods.StreamReadStop(InternalHandle);
        }

        // callbacks ///////////////////////////////////////////////////////////
        // C->C# callbacks need to be static and have MonoPInvokeCallback for
        // IL2CPP builds. avoids "System.NotSupportedException: To marshal a
        // managed method please add an attribute named 'MonoPInvokeCallback'"
        [MonoPInvokeCallback(typeof(uv_watcher_cb))]
        static void OnConnectionCallback(IntPtr handle, int status)
        {
            TcpStream server = HandleContext.GetTarget<TcpStream>(handle);
            if (server == null)
            {
                return;
            }

            TcpStream client = null;
            Exception error = null;
            try
            {
                if (status < 0)
                {
                    error = NativeMethods.CreateError((uv_err_code)status);
                }
                else
                {
                    client = server.NewStream();

                    // initialize internal buffer after we can access libuv
                    // send/recv buffer sizes (which is after connecting)
                    client.InitializeInternalBuffers();
                }

                server.onServerConnect(client, error);
            }
            catch
            {
                client?.Dispose();
                throw;
            }
        }

        // simply callback for message framing that calls onMessage
        void OnFramingCallback(ArraySegment<byte> segment)
        {
            onMessage(this, segment);
        }

        void OnReadCallback(byte[] buffer, int status)
        {
            //Log.Info($"OnReadCallback {HandleType} {InternalHandle} status={status} buffer={byteBuffer.ReadableBytes}");

            // status >= 0 means there is data available
            if (status >= 0)
            {
                //Log.Info($"{HandleType} {InternalHandle} read, buffer length = {byteBuffer.Capacity} status = {status}.");

                // bytes to read within buffer length?
                if (status <= buffer.Length)
                {
                    // framing: we will receive bytes from a stream and we need
                    // to cut it back into messages manually based on header.
                    if (!Framing.Unframe(framingBuffer, ref framingBufferPosition, new ArraySegment<byte>(buffer, 0, status), MaxMessageSize, FramingCallback))
                    {
                        // framing failed because of invalid data / exploits / attacks
                        // let's disconnect
                        Log.Warning($"Unframe failed because of invalid data, potentially because of a header attack. Disconnecting {handle}");

                        // TODO reuse with below code

                        onError(this, new Exception("Unframe failed"));

                        // call onCompleted either way, because we are done reading
                        onCompleted?.Invoke(this);

                        // close the handle
                        CloseHandle();

                        // stop reading either way
                        ReadStop();
                    }
                }
                else Log.Error($"OnReadCallback failed because buffer with length {buffer.Length} is too small for status={status}");
            }
            // status < 0 means error
            else
            {
                // did we encounter a serious error?
                // (UV_EOF means stream was closed, in which case we should call
                //  onCompleted but we don't need to call on onError because
                //  closing a stream is normal behaviour)
                if (status != (int)uv_err_code.UV_EOF)
                {
                    Exception exception = NativeMethods.CreateError((uv_err_code)status);
                    Log.Error($"{HandleType} {InternalHandle} read error, status = {status}: {exception}");
                    onError(this, exception);
                }

                Log.Info($"{HandleType} {InternalHandle} stream completed");

                // call onCompleted either way, because we are done reading
                onCompleted?.Invoke(this);

                // close the handle
                CloseHandle();

                // stop reading either way
                ReadStop();
            }
        }

        // C->C# callbacks need to be static and have MonoPInvokeCallback for
        // IL2CPP builds. avoids "System.NotSupportedException: To marshal a
        // managed method please add an attribute named 'MonoPInvokeCallback'"
        [MonoPInvokeCallback(typeof(uv_read_cb))]
        static void OnReadCallback(IntPtr handle, IntPtr nread, ref uv_buf_t buf)
        {
            TcpStream stream = HandleContext.GetTarget<TcpStream>(handle);
            stream.OnReadCallback(stream.readBuffer, (int)nread.ToInt64());
        }

        // C->C# callbacks need to be static and have MonoPInvokeCallback for
        // IL2CPP builds. avoids "System.NotSupportedException: To marshal a
        // managed method please add an attribute named 'MonoPInvokeCallback'"
        [MonoPInvokeCallback(typeof(uv_alloc_cb))]
        static void OnAllocateCallback(IntPtr handle, IntPtr suggestedSize, out uv_buf_t buf)
        {
            TcpStream stream = HandleContext.GetTarget<TcpStream>(handle);
            buf = stream.readBufferStruct;
        }

        // cleanup /////////////////////////////////////////////////////////////
        internal void OnHandleClosed()
        {
            try
            {
                handle.SetInvalid();
                closeCallback?.Invoke(this);
            }
            catch (Exception exception)
            {
                Log.Error($"{HandleType} close handle callback error: {exception}");
            }
            finally
            {
                closeCallback = null;
                UserToken = null;
            }
        }

        public void CloseHandle()
        {
            // prepare a function that calls onClosed and then
            // disposes in any case, so we can't miss it
            Action<TcpStream> handler = state =>
            {
                onClosed?.Invoke(state);
                // note: Dispose calls CloseHandle() again, but it's fine
                // because ScheduleClose does nothing if not valid anymore and
                // this way we will NEVER forget to call Dipose after closing!
                Dispose();
            };
            closeCallback = handler;

            try
            {
                if (IsValid)
                {
                    UnpinReadBuffer();
                    handle.Dispose();
                }
            }
            catch (Exception exception)
            {
                Log.Error($"{HandleType} Failed to close handle: {exception}");
                throw;
            }
        }

        public void Dispose()
        {
            connectRequest?.Dispose();
            shutdownRequest?.Dispose();

            UnpinReadBuffer();
            try
            {
                CloseHandle();
            }
            catch (Exception exception)
            {
                Log.Warning($"{handle} Failed to close and releasing resources: {exception}");
            }
        }

        // shutdown is the recommended way to end a connection.
        // see also: https://libuv.narkive.com/jDoDEnHm/what-is-the-right-way-to-close-tcp-connections
        //
        // BUT: our tests (e.g. ClientInvoluntaryDisconnect) fail when using
        //      Shutdown instead of CloseHandle. so let's not use it for now.
        //
        // AND: Unity crashes if we use Shutdown instead of CloseHandle, and
        //      then run all tests & recompile once.
        /*
        void ShutdownCallback(WatcherRequest request, Exception error)
        {
            try
            {
                onShutdown?.Invoke(this, error);
            }
            catch (Exception exception)
            {
                Log.Error($"UV_SHUTDOWN callback error: {exception}");
            }

            // close handle after shutdown
            CloseHandle();

            // clean up the request
            shutdownRequest?.Dispose();
            shutdownRequest = null;
        }

        public void Shutdown()
        {
            if (!IsValid)
            {
                return;
            }

            // only if not shutting down already
            if (shutdownRequest == null)
            {
                // create request
                try
                {
                    shutdownRequest = new WatcherRequest(
                        uv_req_type.UV_SHUTDOWN,
                        ShutdownCallback,
                        h => NativeMethods.Shutdown(h, InternalHandle),
                        closeOnCallback: true);
                }
                catch (Exception exception)
                {
                    Exception error = exception;

                    ErrorCode? errorCode = (error as OperationException)?.ErrorCode;
                    if (errorCode == ErrorCode.EPIPE)
                    {
                        // It is ok if the stream is already down
                        error = null;
                    }
                    if (error != null)
                    {
                        Log.Error($"{HandleType} {InternalHandle} failed to shutdown: {error}");
                    }

                    ShutdownCallback(shutdownRequest, error);
                }
            }
        }
        */
    }
}
