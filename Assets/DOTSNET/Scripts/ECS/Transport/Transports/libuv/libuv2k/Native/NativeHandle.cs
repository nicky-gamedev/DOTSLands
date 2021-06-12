// IntPtr handle + Dispose logic so inheriting classes can reuse it.
using System;

namespace libuv2k.Native
{
    public abstract class NativeHandle : IDisposable
    {
        public IntPtr Handle = IntPtr.Zero;

        internal bool IsValid => Handle != IntPtr.Zero;
        internal void SetInvalid() => Handle = IntPtr.Zero;

        protected internal void Validate()
        {
            if (!IsValid)
            {
                throw new ObjectDisposedException($"{GetType()}");
            }
        }

        protected abstract void CloseHandle();

        void Dispose(bool disposing)
        {
            try
            {
                if (!IsValid)
                {
                    return;
                }

                //Log.Info($"Disposing {Handle} (Finalizer {!disposing})");
                CloseHandle();
            }
            catch (Exception exception)
            {
                Log.Error($"{nameof(NativeHandle)} {Handle} error whilst closing handle: {exception}");

                // For finalizer, we cannot allow this to escape.
                if (disposing) throw;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~NativeHandle()
        {
            // in Unity, destructor is called after domain reload (= after
            // changing a script and recompiling).
            // if we forgot to call CloseHandle, then Dispose() will close it
            // again.
            //
            // we ran the game / tests a long time ago and cleanup is only
            // attempted just now after domain reload. this works, but it's just
            // not very clean. we should call Dipose() when we are done with the
            // handle instead.
            //
            // for example, we might declare a 'public Loop loop = new Loop()'
            // but forget to Dispose it in Destroy(), which means that it would
            // take until domain reload -> destructor here for it to happen.
            //
            // in other words, let's show a warning that we forgot to call
            // Dispose (if still valid)
            if (IsValid)
            {
                Log.Warning($"Forgot to Dispose NativeHandle {Handle}. Disposing it now in destructor (likely after domain reload). Make sure to find out where a handle was created without Disposing it, and then Dispose it in all cases to avoid Disposing way later after domain reload, which could cause unexpected behaviour.");
            }

            Dispose(false);
        }
    }
}
