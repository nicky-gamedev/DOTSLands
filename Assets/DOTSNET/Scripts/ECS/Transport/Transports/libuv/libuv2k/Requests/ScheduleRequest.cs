using System;
using libuv2k.Native;

namespace libuv2k
{
    public abstract class ScheduleRequest : IDisposable
    {
        internal ScheduleRequest(uv_req_type requestType)
        {
            RequestType = requestType;
        }

        public bool IsValid => InternalHandle != IntPtr.Zero;

        public object UserToken { get; set; }

        internal abstract IntPtr InternalHandle { get; }

        internal uv_req_type RequestType { get; }

        protected abstract void Close();

        public override string ToString() => $"{RequestType} {InternalHandle}";

        public void Dispose()
        {
            if (!IsValid)
            {
                return;
            }

            UserToken = null;
            Close();
        }
    }
}
