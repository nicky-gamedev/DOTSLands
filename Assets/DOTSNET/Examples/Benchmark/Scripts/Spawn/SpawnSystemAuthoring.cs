using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace DOTSNET.Examples.Benchmark
{
    public class SpawnSystemAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        // spawn prefab
        public NetworkEntityAuthoring spawnPrefab;

        // the system
        SpawnSystem system =>
            Bootstrap.ServerWorld.GetExistingSystem<SpawnSystem>();

        // add system if Authoring is used
        public Type GetSystemType() => typeof(SpawnSystem);

        // configuration
        public int spawnAmount = 10000;
        public float interleave = 1;

        // apply configuration
        void Awake()
        {
            system.spawnPrefabId = Conversion.GuidToBytes16(spawnPrefab.prefabId);
            system.spawnAmount = spawnAmount;
            system.interleave = interleave;
        }
    }

    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    [ServerWorld]
    [UpdateInGroup(typeof(ServerActiveSimulationSystemGroup))]
    public class SpawnSystem : SystemBase
    {
        // dependencies
        [AutoAssign] protected NetworkServerSystem server;
        [AutoAssign] protected PrefabSystem prefabs;

        public Bytes16 spawnPrefabId;
        public int spawnAmount;
        public float interleave;

        public void SpawnAll()
        {
            // get the ECS prefab
            if (prefabs.Get(spawnPrefabId, out Entity prefab))
            {
                // calculate sqrt so we can spawn N * N = Amount
                float sqrt = math.sqrt(spawnAmount);

                // calculate spawn xz start positions
                // based on spawnAmount * distance
                float offset = -sqrt / 2 * interleave;

                // spawn exactly the amount, not one more.
                int spawned = 0;
                for (int spawnX = 0; spawnX < sqrt; ++spawnX)
                {
                    for (int spawnZ = 0; spawnZ < sqrt; ++spawnZ)
                    {
                        // spawn exactly the amount, not any more
                        // (our sqrt method isn't 100% precise)
                        if (spawned < spawnAmount)
                        {
                            Entity entity = EntityManager.Instantiate(prefab);
                            float x = offset + spawnX * interleave;
                            float z = offset + spawnZ * interleave;
                            float3 position = new float3(x, 0, z);
                            SetComponent(entity, new Translation{Value = position});

                            // spawn it on all clients, owned by no one
                            server.Spawn(entity, null);

                            ++spawned;
                        }
                    }
                }
            }
            else Debug.LogError("Failed to find Spawn prefab. Was it added to the PrefabSystem's spawnable prefabs list?");
        }

        protected override void OnStartRunning()
        {
            // spawn when the server starts
            SpawnAll();
        }

        protected override void OnUpdate() {}
    }
}