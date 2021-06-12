// some server systems should only be updated while the server is active.
//
// for example:
//   * Transport: update all the time so we can actually start/stop the server
//   * MonsterMovement: update only while server is running
//
// usage:
//   add the UpdateInGroup attribute to a system:
//     [UpdateInGroup(typeof(ServerActiveSimulationSystemGroup))]
//   OnStartRunning / OnUpdate will only be called when the server is active!
//   OnStopRunning will be called when the server stops being active!
using Unity.Entities;

namespace DOTSNET
{
    // [ServerWorld] adds it to server world automatically. no bootstrap needed!
    [ServerWorld]
    [AlwaysUpdateSystem]
    // Systems in group may need to apply physics, so update in the safe group
    [UpdateInGroup(typeof(ApplyPhysicsGroup))]
    public class ServerActiveSimulationSystemGroup : ComponentSystemGroup
    {
        // dependencies
        [AutoAssign] protected NetworkServerSystem server;

        protected override void OnUpdate()
        {
            // enable/disable systems based on server state
            // IMPORTANT: we need to set .Enabled to false after StopServer,
            //            otherwise OnStopRunning is never called in the group's
            //            systems. trying to call OnUpdate only while ACTIVE
            //            would not call OnStopRunning after StopServer.
            foreach (ComponentSystemBase system in m_systemsToUpdate)
            {
                // with ?. null check to disable all systems if there is no
                // server. someone might not have a dotsnet scene open, or
                // someone might be working on a client-only addon.
                system.Enabled = server?.state == ServerState.ACTIVE;
            }

            // always call base OnUpdate, otherwise nothing is updated again
            base.OnUpdate();
        }
    }
}