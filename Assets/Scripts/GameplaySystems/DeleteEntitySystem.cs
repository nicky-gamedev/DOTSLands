using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Reese.Nav;
using Reese.Spawning;
using DOTSNET;
using System.Collections.Generic;
using Unity.Jobs;
using System.Linq;

[ServerWorld]
public class DeleteSoldierSystem : NetworkServerMessageSystem<DeleteSoldier>
{
    protected override void OnMessage(int connectionId, DeleteSoldier message)
    {
        if (!server.spawned.TryGetValue(message.soliderID, out Entity entity)) return;
        server.Destroy(entity);
    }

    protected override void OnUpdate() { }

    protected override bool RequiresAuthentication() { return true; }
}

public struct DeleteSoldier : NetworkMessage
{
    public ulong soliderID;
    public ushort GetID() { return 0x2009; }
}

[ClientWorld]
public class SendEntitiesToDelete : SystemBase
{
    NativeList<DeleteSoldier> messages;
    [AutoAssign] protected NetworkClientSystem client;
    protected override void OnCreate()
    {
        base.OnCreate();
        messages = new NativeList<DeleteSoldier>(Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        messages.Dispose();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = World.GetExistingSystem<BeginInitializationEntityCommandBufferSystem>().CreateCommandBuffer();
        EntityQuery query = GetEntityQuery(typeof(SelectedTroopComponent));

        var _messages = messages;
        var array = query.ToEntityArray(Allocator.TempJob);

        Entities.ForEach((Entity e , in DeleteSelectedEntity delete) =>
        {
            for (int i = 0; i < array.Length; i++)
            {
                _messages.Add(new DeleteSoldier { soliderID = GetComponent<NetworkEntity>(array[i]).netId });
            }

            ecb.RemoveComponent<DeleteSelectedEntity>(e);
        }).WithoutBurst().Run();

        Debug.Log(_messages.Length);
        array.Dispose();
        client.Send(_messages);
        messages.Clear();
    }
}
