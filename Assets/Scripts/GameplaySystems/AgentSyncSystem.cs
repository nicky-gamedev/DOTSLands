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

[ClientWorld]
[UpdateInGroup(typeof(ClientConnectedSimulationSystemGroup))]
public class AgentSyncSystem : SystemBase
{
    BeginSimulationEntityCommandBufferSystem bufferSystem;

    protected override void OnStartRunning()
    {
        bufferSystem = Bootstrap.ClientWorld.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        var component = GetComponentDataFromEntity<PlayerTag>();
        var agents = GetEntityQuery(typeof(PlayerAgentComponent)).ToEntityArray(Allocator.TempJob);
        foreach (var item in agents)
        {
            if (!GetComponent<SyncTag>(item).canSync) continue;

            if (!component.HasComponent(GetComponent<PlayerAgentComponent>(item).player))
                SetComponent(item, new PlayerAgentComponent { player = Entity.Null, ID = 0, destroyMe = true });
        }
        agents.Dispose();

        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback);
        Entities.ForEach(
            (Entity e, int entityInQueryIndex, in LocalToWorld local, in PlayerAgentComponent player, in SyncTag sync) =>
            {
                if (!sync.canSync) return;

                if (player.destroyMe)
                {
                    ecb.DestroyEntity(e);
                    return;
                }

                var t = new LocalToWorld();
                t.Value = local.Value;
                float3 pos = t.Position;
                ecb.SetComponent(player.player, new Translation { Value = pos });
            }).Run();
        try
        {
            ecb.Playback(EntityManager);
        }
        catch
        {
            Debug.Log("An entity couldn't be updated, skipping a frame");
        }

        ecb.Dispose();
    }
}
