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
public class AgentSpawnSystem : SystemBase
{
    Entity prefabEntity;
    float3 spawnPos;
    BeginSimulationEntityCommandBufferSystem bufferSystem;

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        prefabEntity = EntityManager.CreateEntityQuery(typeof(SpawnNavAgentComponent)).GetSingleton<SpawnNavAgentComponent>().Value;
        bufferSystem = Bootstrap.ClientWorld.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
        
    }

    protected override void OnUpdate()
    {
        if(math.all(spawnPos == float3.zero))
        {
            var query = GetEntityQuery(typeof(PlayerDataComponent), typeof(NetworkEntity)).ToEntityArray(Allocator.TempJob);
            foreach (var item in query)
            {
                if (GetComponent<NetworkEntity>(item).owned)
                {
                    spawnPos = GetComponent<LocalToWorld>(item).Position;
                    Debug.Log("player found!");
                }
            }
            query.Dispose();
        }
        EntityCommandBuffer ecb = bufferSystem.CreateCommandBuffer();
        var needsAgentQuery = GetEntityQuery(typeof(NeedsAgent)).ToEntityArray(Allocator.TempJob);
        var needsPlayerQuery = GetEntityQuery(typeof(NeedsPlayer)).ToEntityArray(Allocator.TempJob);

        for (int i = 0; i < needsAgentQuery.Length; i++)
        {
            if (needsAgentQuery.Length != needsPlayerQuery.Length &&
                HasComponent<NeedsAgent>(needsAgentQuery[i]) &&
                GetComponent<NetworkEntity>(needsAgentQuery[i]).owned)
            {
                SpawnSystem.Enqueue(new Spawn()
                    .WithPrefab(prefabEntity)
                    .WithComponentList(
                        new NavAgent
                        {
                            JumpDegrees = 45,
                            JumpGravity = 100,
                            JumpSpeedMultiplierX = 2,
                            JumpSpeedMultiplierY = 4,
                            TranslationSpeed = 5,
                            RotationSpeed = 0.05f,
                            TypeID = NavUtil.GetAgentType(NavConstants.HUMANOID),
                            Offset = new float3(0, 1, 0)
                        },
                        new Parent { },
                        new LocalToParent { },
                        new LocalToWorld
                        {
                            Value = float4x4.TRS(
                                spawnPos,
                                quaternion.identity,
                                1
                            )
                        },
                        new Translation
                        {
                            Value = spawnPos
                        },
                        new Rotation { },
                        new NavNeedsSurface { },
                        new NavTerrainCapable { },
                        new NeedsPlayer { }
                    )
                );
            }
            else
            {
                ecb.RemoveComponent(needsAgentQuery[i], typeof(NeedsAgent));
            }
        }

        for (int i = 0; i < needsPlayerQuery.Length; i++)
        {
            ecb.AddComponent(needsAgentQuery[i], new AgentPlayerComponent { agent = needsPlayerQuery[i] });
            ecb.AddComponent(needsPlayerQuery[i], new PlayerAgentComponent
            {
                player = needsAgentQuery[i],
                ID = GetComponent<NetworkEntity>(needsAgentQuery[i]).netId
            });
            ecb.AddComponent(needsPlayerQuery[i], new SyncTag { canSync = true });
            ecb.RemoveComponent(needsPlayerQuery[i], typeof(NeedsPlayer));
        }

        needsAgentQuery.Dispose();
        needsPlayerQuery.Dispose();
    }
}

public struct SyncTag : IComponentData
{
    public bool canSync;
}