// TransportClientSystem and TransportServerSystem both inherit from
// TransportSystem. This way we can add some common functionality without
// writing twice the code.
using Unity.Entities;

namespace DOTSNET
{
    // AlwaysUpdate is a good idea. we should never stop updating a transport.
    [AlwaysUpdateSystem]
    // Transport messages may need to apply physics, so update in the safe group
    [UpdateInGroup(typeof(ApplyPhysicsGroup))]
    // IMPORTANT: use [DisableAutoCreation] + SelectiveSystemAuthoring when
    //            inheriting
    public abstract class TransportSystem : SystemBase
    {
        // check if Transport is Available on this platform
        public abstract bool Available();

        // get max packet size that the transport can send at once
        public abstract int GetMaxPacketSize();

        // find first available TransportSystem on this platform
        public static TransportSystem FindAvailable(World world)
        {
            foreach (ComponentSystemBase system in world.Systems)
                if (system is TransportSystem transport && transport.Available())
                    return transport;
            return null;
        }
    }
}