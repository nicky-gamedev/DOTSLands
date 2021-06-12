// some client systems should only be updated while the client is connected.
//
// for example:
//   * Transport: update all the time so we can actually (dis)connect the client
//   * LocalPlayerMovement: update only while client is running
//
// usage:
//   add the UpdateInGroup attribute to a system:
//     [UpdateInGroup(typeof(ClientConnectedSimulationSystemGroup))]
//   OnStartRunning / OnUpdate will only be called when the client is connected!
//   OnStopRunning will be called when the client disconnected!
using Unity.Entities;

namespace DOTSNET
{
    // [ClientWorld] adds it to client world automatically. no bootstrap needed!
    [ClientWorld]
    [AlwaysUpdateSystem]
    // Systems in group may need to apply physics, so update in the safe group
    [UpdateInGroup(typeof(ApplyPhysicsGroup))]
    public class ClientConnectedSimulationSystemGroup : ComponentSystemGroup
    {
        // dependencies
        [AutoAssign] protected NetworkClientSystem client;

        protected override void OnUpdate()
        {
            // enable/disable systems based on client state
            // IMPORTANT: we need to set .Enabled to false after Disconnect,
            //            otherwise OnStopRunning is never called in the group's
            //            systems. trying to call OnUpdate only while CONNECTED
            //            would not call OnStopRunning after disconnected.
            foreach (ComponentSystemBase system in m_systemsToUpdate)
            {
                // with ?. null check to disable all systems if there is no
                // client. someone might not have a dotsnet scene open, or
                // someone might be working on a server-only addon.
                system.Enabled = client?.state == ClientState.CONNECTED;
            }

            // always call base OnUpdate, otherwise nothing is updated again
            base.OnUpdate();
        }
    }
}