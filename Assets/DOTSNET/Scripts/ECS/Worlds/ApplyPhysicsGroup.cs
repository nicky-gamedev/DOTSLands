// a group that updates at the safest time for Unity.Physics.
// updating before/after the wrong Unity.Physics system causes errors otherwise.
//
// we have several systems that should be updated safely for potential physics:
//   * ServerActive/ClientConnectedSimulationSystemGroup may run physics
//   * Transports may run physics, e.g. if a message should apply force
//   * NetworkServer/Client may run physics, e.g. if it processes messages in
//     the future
using Unity.Entities;
// #if UNITY_PHYSICS would be nice, but there is no such define yet.
using Unity.Physics.Systems;
// #endif

namespace DOTSNET
{
    [ServerWorld, ClientWorld]
    // IMPORTANT: update before BuildPhysicsWorld, otherwise systems in this
    //            group can't apply physics. see also:
    //            https://forum.unity.com/threads/unity-physics-exportphysicsworld-exportdynamicbodiesjob-execute-indexoutofrangeexception.890221/
    [UpdateBefore(typeof(BuildPhysicsWorld))]
    public class ApplyPhysicsGroup : ComponentSystemGroup {}
}