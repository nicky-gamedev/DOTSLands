// Base class for all server transports.
using System;
using Unity.Entities;
using UnityEngine;

namespace DOTSNET
{
    [ServerWorld]
    // TransportServerSystem should be updated AFTER all other server systems.
    // we need a guaranteed update order to avoid race conditions where it might
    // randomly be updated before other systems, causing all kinds of unexpected
    // effects. determinism is always a good idea!
    //
    // Note: we update AFTER everything else, not before. This way systems like
    //       NetworkServerSystem can apply configurations in OnStartRunning, and
    //       Transport OnStartRunning is called afterwards, not before. Other-
    //       wise the OnData etc. events wouldn't be hooked up.
    //
    // * [UpdateAfter(NetworkServerSystem)] won't work because in some cases
    //   like Pong, we inherit from NetworkServerSystem and the UpdateAfter tag
    //   won't find the inheriting class. see also:
    //   https://forum.unity.com/threads/updateafter-abstractsystemtype-wont-update-after-inheritingsystemtype-abstractsystemtype.915170/
    // * [UpdateInGroup(ApplyPhysicsGroup), OrderLast=true] would be fine, but
    //   it doesn't actually update last for some reason. could be our custom
    //   Bootstrap, or a Unity bug.
    //
    // The best solution is to update in LateSimulationSystemGroup. That's what
    // it's for anyway.
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public abstract class TransportServerSystem : TransportSystem
    {
        // events //////////////////////////////////////////////////////////////
        // NetworkServerSystem should hook into this to receive events.
        // Fallback/Multiplex transports could also hook/route those as needed.
        // => Data ArraySegments are only valid until next call, so process the
        //    events immediately!
        // => We don't call NetworkServerSystem.OnTransportConnected etc.
        //    directly. This way we have less dependencies, and it's easier to
        //    test!
        // => LogWarning by default for testing, and so it's obvious that the
        //    actions still need to be replaced.
        // IMPORTANT: call them from main thread!
        public Action<int> OnConnected =
            (connectionId) => { Debug.LogWarning("TransportServerSystem.OnServerConnected: " + connectionId); };

        public Action<int, ArraySegment<byte>> OnData =
            (connectionId, segment) => { Debug.LogWarning("TransportServerSystem.OnServerData: " + connectionId + " => " + BitConverter.ToString(segment.Array, segment.Offset, segment.Count)); };

        public Action<int> OnDisconnected =
            (connectionId) => { Debug.LogWarning("TransportServerSystem.OnServerDisconnected: " + connectionId); };

        // abstracts ///////////////////////////////////////////////////////////
        // check if server is running
        public abstract bool IsActive();

        // start listening
        public abstract void Start();

        // send ArraySegment to the client with connectionId
        // note: DOTSNET already packs messages. Transports don't need to.
        public abstract bool Send(int connectionId, ArraySegment<byte> segment, Channel channel);

        // disconnect one client from the server
        public abstract bool Disconnect(int connectionId);

        // get a connection's IP address
        public abstract string GetAddress(int connectionId);

        // stop the server
        public abstract void Stop();
    }
}