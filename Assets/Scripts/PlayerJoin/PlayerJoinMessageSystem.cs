using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using DOTSNET;
using Unity.Collections;

[DisableAutoCreation]
public class PlayerJoinMessageSystem : NetworkServerMessageSystem<PlayerJoinMessage>
{
    [AutoAssign] protected PrefabSystem prefabSystem;

    public NativeList<float3> positions;

    protected override void OnCreate()
    {
        base.OnCreate();
        positions = new NativeList<float3>(Allocator.Persistent);
    }

    protected override void OnMessage(int connectionId, PlayerJoinMessage message)
    {
        if (prefabSystem.Get(message.playerPrefabId, out Entity prefab))
        {
            Entity player = EntityManager.Instantiate(prefab);
            int playersInServer = GetEntityQuery(typeof(PlayerDataComponent)).CalculateEntityCount();
            SetComponent(player, new Translation { Value = positions[playersInServer - 1]});
            server.JoinWorld(connectionId, player);
        }
    }

    protected override void OnUpdate() { }

    protected override bool RequiresAuthentication() { return true; }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        positions.Dispose();
    }
}
