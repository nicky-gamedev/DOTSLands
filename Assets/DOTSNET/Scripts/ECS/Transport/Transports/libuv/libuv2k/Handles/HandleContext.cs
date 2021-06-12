using AOT;
using System;
using System.Runtime.InteropServices;
using libuv2k.Native;

namespace libuv2k
{
    internal sealed unsafe class HandleContext : NativeHandle
    {
        static readonly uv_close_cb CloseCallback = OnCloseHandle;
        internal readonly uv_handle_type handleType;

        internal HandleContext(
            uv_handle_type handleType,
            Func<IntPtr, IntPtr, int> initializer,
            IntPtr loopHandle,
            TcpStream target)
        {
            if (loopHandle != IntPtr.Zero && initializer != null && target != null)
            {
                int size = NativeMethods.GetSize(handleType);
                IntPtr handle = Marshal.AllocCoTaskMem(size);

                try
                {
                    int result = initializer(loopHandle, handle);
                    NativeMethods.ThrowIfError(result);
                }
                catch (Exception)
                {
                    Marshal.FreeCoTaskMem(handle);
                    throw;
                }

                GCHandle gcHandle = GCHandle.Alloc(target, GCHandleType.Normal);
                ((uv_handle_t*)handle)->data = GCHandle.ToIntPtr(gcHandle);

                Handle = handle;
                this.handleType = handleType;
                //Logger.Log($"{handleType} {handle} allocated");
            }
            else throw new ArgumentException($"loopHandle {loopHandle}, initializer {initializer}, target {target} can't be null!");
        }

        internal bool IsActive => IsValid && NativeMethods.IsHandleActive(Handle);

        internal bool IsClosing => IsValid && NativeMethods.IsHandleClosing(Handle);

        protected override void CloseHandle()
        {
            if (Handle != IntPtr.Zero)
            {
                NativeMethods.CloseHandle(Handle, CloseCallback);
                //Logger.Log($"{handleType} {handle} closed, releasing pending resources.");
            }
        }

        internal static T GetTarget<T>(IntPtr handle)
        {
            if (handle != IntPtr.Zero)
            {
                IntPtr internalHandle = ((uv_handle_t*)handle)->data;
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

        // C->C# callbacks need to be static and have MonoPInvokeCallback for
        // IL2CPP builds. avoids "System.NotSupportedException: To marshal a
        // managed method please add an attribute named 'MonoPInvokeCallback'"
        [MonoPInvokeCallback(typeof(uv_close_cb))]
        static void OnCloseHandle(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                return;
            }

            TcpStream scheduleHandle = null;

            // Get gc handle first
            IntPtr pHandle = ((uv_handle_t*)handle)->data;
            if (pHandle != IntPtr.Zero)
            {
                GCHandle nativeHandle = GCHandle.FromIntPtr(pHandle);
                if (nativeHandle.IsAllocated)
                {
                    scheduleHandle = nativeHandle.Target as TcpStream;
                    nativeHandle.Free();

                    ((uv_handle_t*)handle)->data = IntPtr.Zero;
                }
            }

            // Release memory
            Marshal.FreeCoTaskMem(handle);
            scheduleHandle?.OnHandleClosed();
        }
    }
}
