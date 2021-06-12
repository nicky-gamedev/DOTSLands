// Entities can have 0..n observer connections to broadcast to.
// The buffer is added during NetworkEntity authoring.
// The buffer is modified by the InterestManagementSystem.
using Unity.Entities;

namespace DOTSNET
{
    // let's use 16 for initial capacity. most entities won't have more observers
    // than that. (it still scales up if needed)
    [InternalBufferCapacity(16)]
    public struct NetworkObserver : IBufferElementData
    {
        // implicit conversions to reduce typing
        public static implicit operator int(NetworkObserver e) { return e.connectionId; }
        public static implicit operator NetworkObserver(int e) { return new NetworkObserver { connectionId = e }; }

        // actual value each buffer element will store.
        public int connectionId;

        // GetHashCode needed for DynamicBuffer.Contains equality check in Jobs
        public override int GetHashCode() => connectionId;
    }

    // we need an addition rebuild buffer for easier to develop interest
    // management Jobs.
    // using a helper NativeArray works too, but there is an "Invalid IL Code"
    // bug that needs to be fixed by Unity. Until then, we use yet another
    // buffer.
    [InternalBufferCapacity(16)]
    public struct RebuildNetworkObserver : IBufferElementData
    {
        // implicit conversions to reduce typing
        public static implicit operator int(RebuildNetworkObserver e) { return e.connectionId; }
        public static implicit operator RebuildNetworkObserver(int e) { return new RebuildNetworkObserver { connectionId = e }; }

        // actual value each buffer element will store.
        public int connectionId;

        // GetHashCode needed for DynamicBuffer.Contains equality check in Jobs
        public override int GetHashCode() => connectionId;
    }
}
