// Client sends JoinWorldMessage to the server to indicate that it's finished
// selecting characters / being in lobby, and is ready to join the world now.
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DOTSNET.Examples.Benchmark
{
    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class JoinWorldMessageSystem : NetworkServerMessageSystem<JoinWorldMessage>
    {
        // dependencies
        [AutoAssign] protected PrefabSystem prefabSystem;

        // spawn position is set by SpawnPositionAuthoring
        public float3 spawnPosition;

        protected override void OnUpdate() {}
        protected override bool RequiresAuthentication() { return true; }
        protected override void OnMessage(int connectionId, JoinWorldMessage message)
        {
            // find prefab with that prefabId
            // note: this is just a simple example. in a real project we should
            //       check if the assetId is actually a player prefab, not a
            //       monster prefab etc.
            if (prefabSystem.Get(message.playerPrefabId, out Entity prefab))
            {
                // instantiate player prefab
                Entity player = EntityManager.Instantiate(prefab);

                // apply spawn position
                SetComponent(player, new Translation{Value=spawnPosition});

                // join the world with that player prefab
                server.JoinWorld(connectionId, player);
            }
        }
    }
}