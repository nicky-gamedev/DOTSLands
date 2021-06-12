using System;
using System.Runtime.InteropServices;
using libuv2k.Native;

namespace libuv2k
{
    sealed unsafe class RequestContext : NativeHandle
    {
        readonly uv_req_type requestType;
        public readonly int HandleSize;

        internal RequestContext(uv_req_type requestType, int size, ScheduleRequest target)
        {
            if (size >= 0 && target != null)
            {
                HandleSize = NativeMethods.GetSize(requestType);
                int totalSize = HandleSize + size;
                IntPtr handle = Marshal.AllocCoTaskMem(totalSize);

                GCHandle gcHandle = GCHandle.Alloc(target, GCHandleType.Normal);
                *(IntPtr*)handle = GCHandle.ToIntPtr(gcHandle);

                Handle = handle;
                this.requestType = requestType;
            }
            else throw new ArgumentException($"Size {size} needs to be >=0 and target {target} can't be null.");
        }

        internal RequestContext(uv_req_type requestType, Action<IntPtr> initializer, ScheduleRequest target)
        {
            if (initializer != null && target != null)
            {
                HandleSize = NativeMethods.GetSize(requestType);
                IntPtr handle = Marshal.AllocCoTaskMem(HandleSize);

                try
                {
                    initializer(handle);
                }
                catch
                {
                    Marshal.FreeCoTaskMem(handle);
                    throw;
                }

                GCHandle gcHandle = GCHandle.Alloc(target, GCHandleType.Normal);
                *(IntPtr*)handle = GCHandle.ToIntPtr(gcHandle);

                Handle = handle;
                this.requestType = requestType;
                //Log.Info($"{requestType} {handle} allocated.");
            }
            else throw new ArgumentException($"initializer {initializer} and target {target} can't be null.");
        }

        internal static T GetTarget<T>(IntPtr handle)
        {
            if (handle != IntPtr.Zero)
            {
                IntPtr internalHandle = ((uv_req_t*)handle)->data;
                if (internalHandle != IntPtr.Zero)
                {
                    GCHandle gcHandle = GCHandle.FromIntPtr(internalHandle);
                    if (gcHandle.IsAllocated)
                    {
                        return (T)gcHandle.Target;
                    }
                }
            }

            return default(T);
        }

        protected override void CloseHandle()
        {
            IntPtr handle = Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            IntPtr pHandle = ((uv_req_t*)handle)->data;

            // Free GCHandle
            if (pHandle != IntPtr.Zero)
            {
                GCHandle nativeHandle = GCHandle.FromIntPtr(pHandle);
                if (nativeHandle.IsAllocated)
                {
                    nativeHandle.Free();
                    ((uv_req_t*)handle)->data = IntPtr.Zero;
                    Log.Info($"{requestType} {handle} GCHandle released.");
                }
            }

            // Release memory
            Marshal.FreeCoTaskMem(handle);
            Handle = IntPtr.Zero;

            Log.Info($"{requestType} {handle} memory released.");
        }
    }
}
