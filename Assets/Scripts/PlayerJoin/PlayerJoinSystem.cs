using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using DOTSNET;

[ClientWorld]
[UpdateInGroup(typeof(ClientConnectedSimulationSystemGroup))]
[DisableAutoCreation]
public class PlayerJoinSystem : SystemBase
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
            PlayerJoinMessage message = new PlayerJoinMessage(prefabId);
            client.Send(message);
            Debug.Log("AutoJoinWorldSystem: requesting to spawn player with prefabId=" + Conversion.Bytes16ToGuid(prefabId));
        }
        else Debug.LogError("AutoJoinWorldSystem: no registered prefab found to join with.");
    }

    protected override void OnUpdate() { }
}
