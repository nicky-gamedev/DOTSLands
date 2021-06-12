using UnityEngine;
using Unity.Entities;
using DOTSNET;

[ServerWorld]
public class TerrainClaimListener : NetworkServerMessageSystem<ClaimAction>
{
    [AutoAssign] protected PrefabSystem prefab;
    protected override void OnMessage(int connectionId, ClaimAction message)
    {
        Debug.Log("TerrainClaimListener OnMessage()");
        Entity entity;
        if (server.spawned.TryGetValue(message.id, out entity))
        {
            var cc = EntityManager.GetComponentData<ClaimComponent>(entity);
            cc.belongsTo = message.id;
            EntityManager.SetComponentData(entity, cc);
            return;
        }
        Debug.LogError("Error: This entity doesn't exist");
    }

    protected override void OnUpdate() { }

    protected override bool RequiresAuthentication() { return true; } 
}
