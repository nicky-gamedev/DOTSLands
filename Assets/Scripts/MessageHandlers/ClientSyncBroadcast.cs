using UnityEngine;
using Unity.Entities;
using DOTSNET;
using Unity.Collections;

[ClientWorld]
public class ClientSyncBroadcast : NetworkClientMessageSystem<ClaimComponentUpdate>
{
    [AutoAssign] protected PrefabSystem prefab;
    protected override void OnMessage(ClaimComponentUpdate message)
    {
        Entity entity;

        if (client.spawned.TryGetValue(message.id, out entity))
        {
            var cc = EntityManager.GetComponentData<ClaimComponent>(entity);
            cc.belongsTo = message.serverTerrainBelongsTo;
            EntityManager.SetComponentData(entity, cc);
        }
    }

    protected override void OnUpdate() { }
}
