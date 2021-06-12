namespace libuv2k
{
    public static class libuv2k
    {
        // one global Shutdown method to be called after using libuv2k, so the
        // user doesn't need to dispose pool entires manually, etc.
        public static void Shutdown()
        {
            // it's very important that we dispose every WriteRequest in our
            // Pool. otherwise it will take until domain reload for the
            // NativeHandle destructor to be called to clean them up, which
            // wouldn't be very clean.
            TcpStream.WriteRequestPool.Clear();
        }
    }
}