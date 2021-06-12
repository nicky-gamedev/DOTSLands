using AOT;
using System;
using libuv2k.Native;

namespace libuv2k
{
    public sealed class WatcherRequest : ScheduleRequest
    {
        internal static readonly uv_watcher_cb WatcherCallback = OnWatcherCallback;
        readonly RequestContext handle;
        readonly bool closeOnCallback;
        Action<WatcherRequest, Exception> watcherCallback;

        internal WatcherRequest(
            uv_req_type requestType,
            Action<WatcherRequest, Exception> watcherCallback,
            Action<IntPtr> initializer,
            bool closeOnCallback = false)
            : base(requestType)
        {
            if (initializer != null)
            {
                this.watcherCallback = watcherCallback;
                this.closeOnCallback = closeOnCallback;
                handle = new RequestContext(requestType, initializer, this);
            }
            else throw new ArgumentException($"Initializer can't be null!");
        }

        internal override IntPtr InternalHandle => handle.Handle;

        void OnWatcherCallback(OperationException error)
        {
            try
            {
                if (error != null)
                {
                    Log.Error($"{RequestType} {InternalHandle} error : {error.ErrorCode} {error.Name}: {error}");
                }

                watcherCallback?.Invoke(this, error);

                if (closeOnCallback)
                {
                    Dispose();
                }
            }
            catch (Exception exception)
            {
                Log.Error($"{RequestType} {nameof(OnWatcherCallback)} error: {exception}");
                throw;
            }
        }

        // C->C# callbacks need to be static and have MonoPInvokeCallback for
        // IL2CPP builds. avoids "System.NotSupportedException: To marshal a
        // managed method please add an attribute named 'MonoPInvokeCallback'"
        [MonoPInvokeCallback(typeof(uv_watcher_cb))]
        static void OnWatcherCallback(IntPtr handle, int status)
        {
            if (handle == IntPtr.Zero)
            {
                return;
            }

            WatcherRequest request = RequestContext.GetTarget<WatcherRequest>(handle);
            OperationException error = null;
            if (status < 0)
            {
                error = NativeMethods.CreateError((uv_err_code)status);
            }

            request?.OnWatcherCallback(error);
        }

        protected override void Close()
        {
            watcherCallback = null;
            handle.Dispose();
        }
    }
}
