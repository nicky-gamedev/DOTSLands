// this system sends a join-world message to the server as soon as the client
// connected.
// usually we would have a character/team selection UI and join button, but for
// our example we simply join automatically.
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;

namespace DOTSNET.Examples.Physics
{
    [ClientWorld]
    [UpdateInGroup(typeof(ClientConnectedSimulationSystemGroup))]
    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class AutoJoinWorldSystem : SystemBase
    {
        [AutoAssign] protected NetworkClientSystem client;
        [AutoAssign] protected PrefabSystem prefabSystem;

        bool FindFirstRegisteredPrefab(out Bytes16 prefabId, out Entity prefab)
        {
            foreach (KeyValuePair<Bytes16, Entity> kvp in prefabSystem.prefabs)
            {
                prefabId = kvp.Key;
                prefab = kvp.Value;
                return true;
            }
            prefabId = new Bytes16();
            prefab = new Entity();
            return false;
        }

        // OnStartRunning is called after the client connected
        protected override void OnStartRunning()
        {
            // our example only has 1 spawnable prefab. let's use that for the
            // player.
            if (FindFirstRegisteredPrefab(out Bytes16 prefabId, out Entity prefab))
            {
                JoinWorldMessage message = new JoinWorldMessage(prefabId);
                client.Send(message);
                Debug.Log("AutoJoinWorldSystem: requesting to spawn player with prefabId=" + Conversion.Bytes16ToGuid(prefabId));
            }
            else Debug.LogError("AutoJoinWorldSystem: no registered prefab found to join with.");
        }

        protected override void OnUpdate() {}
    }
}
