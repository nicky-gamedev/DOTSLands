// IMPORTANT: call loop.Dispose in OnDestroy to avoid a Unity Editor crash where
//            changing and recompiling a script would cause the disposal to
//            access null pointers and crash the Editor.
using AOT;
using System;
using System.Runtime.InteropServices;
using libuv2k.Native;

namespace libuv2k
{
    public sealed unsafe class Loop : NativeHandle
    {
        static readonly uv_walk_cb WalkCallback = OnWalkCallback;

        public Loop()
        {
            int size = NativeMethods.GetLoopSize();
            Handle = Marshal.AllocCoTaskMem(size);
            try
            {
                NativeMethods.InitializeLoop(Handle);
            }
            catch
            {
                Marshal.FreeCoTaskMem(Handle);
                throw;
            }

            GCHandle gcHandle = GCHandle.Alloc(this, GCHandleType.Normal);
            ((uv_loop_t*)Handle)->data = GCHandle.ToIntPtr(gcHandle);

            Log.Info($"Loop {Handle} allocated.");
        }

        public bool IsAlive => IsValid && NativeMethods.IsLoopAlive(Handle);

        public int Run(uv_run_mode mode)
        {
            Validate();
            return NativeMethods.RunLoop(Handle, mode);
        }

        public void Stop()
        {
            Validate();
            NativeMethods.StopLoop(Handle);
        }

        protected override void CloseHandle()
        {
            if (Handle == IntPtr.Zero)
            {
                return;
            }

            // Get gc handle before close loop
            IntPtr pHandle = ((uv_loop_t*)Handle)->data;

            // Fully close the loop, similar to
            //https://github.com/libuv/libuv/blob/v1.x/test/task.h#L190

            int count = 0;
            while (true)
            {
                //Log.Info($"Loop {Handle} walking handles, count = {count}.");
                NativeMethods.WalkLoop(Handle, WalkCallback);

                // calling CloseHandle calls uv_close.
                // libuv will only close and call the close callbacks next time
                // it is updated, so we need to update it once for everything to
                // be closed properly.
                //Log.Info($"Loop {Handle} running default to call close callbacks, count = {count}.");
                NativeMethods.RunLoop(Handle, uv_run_mode.UV_RUN_DEFAULT);

                //Log.Info($"Loop {Handle} close result = {result}, count = {count}.");
                if (NativeMethods.CloseLoop(Handle) == 0)
                {
                    break;
                }
                count++;
                if (count >= 20)
                {
                    Log.Warning($"Loop {Handle} close all handles limit 20 times exceeded.");
                    break;
                }
            }

            //Logger.Info($"Loop {handle} closed.");

            // Free GCHandle
            if (pHandle != IntPtr.Zero)
            {
                GCHandle nativeHandle = GCHandle.FromIntPtr(pHandle);
                if (nativeHandle.IsAllocated)
                {
                    nativeHandle.Free();
                    ((uv_loop_t*)Handle)->data = IntPtr.Zero;
                    //Logger.Info($"Loop {Handle} GCHandle released.");
                }
            }

            // Release memory
            Marshal.FreeCoTaskMem(Handle);
            //Logger.Info($"Loop {Handle} memory released.");
            Handle = IntPtr.Zero;
        }

        // C->C# callbacks need to be static and have MonoPInvokeCallback for
        // IL2CPP builds. avoids "System.NotSupportedException: To marshal a
        // managed method please add an attribute named 'MonoPInvokeCallback'"
        [MonoPInvokeCallback(typeof(uv_walk_cb))]
        static void OnWalkCallback(IntPtr handle, IntPtr loopHandle)
        {
            if (handle == IntPtr.Zero)
            {
                return;
            }

            try
            {
                IDisposable target = HandleContext.GetTarget<IDisposable>(handle);
                target?.Dispose();
                //Logger.Info($"Loop {loopHandle} walk callback disposed {handle} {target?.GetType()}");
            }
            catch (Exception)
            {
                //Logger.Warn($"Loop {loopHandle} Walk callback attempt to close handle {handle} failed. {exception}");
            }
        }
    }
}
