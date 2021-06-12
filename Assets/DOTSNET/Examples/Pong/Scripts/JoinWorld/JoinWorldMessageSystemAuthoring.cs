using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace DOTSNET.Examples.Pong
{
    public class JoinWorldMessageSystemAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        // system in world
        JoinWorldMessageSystem system =>
            Bootstrap.ServerWorld.GetExistingSystem<JoinWorldMessageSystem>();

        // spawn positions
        public Transform leftSpawnPosition;
        public Transform rightSpawnPosition;

        // add system if Authoring is used
        public Type GetSystemType() => typeof(JoinWorldMessageSystem);

        // apply configuration
        void Awake()
        {
            system.leftSpawnPosition = leftSpawnPosition.position;
            system.rightSpawnPosition = rightSpawnPosition.position;
        }
    }

    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class JoinWorldMessageSystem : NetworkServerMessageSystem<JoinWorldMessage>
    {
        // dependencies
        [AutoAssign] protected PrefabSystem prefabSystem;

        // spawn positions
        public float3 leftSpawnPosition;
        public float3 rightSpawnPosition;

        protected override void OnUpdate() {}
        protected override bool RequiresAuthentication() { return true; }
        protected override void OnMessage(int connectionId, JoinWorldMessage message)
        {
            // only if not two player yet
            PongServerSystem pongServer = (PongServerSystem)server;
            if (!pongServer.leftPlayerExists || !pongServer.rightPlayerExists)
            {
                // find prefab with that prefabId
                // note: this is just a simple example. in a real project we should
                //       check if the assetId is actually a player prefab, not a
                //       monster prefab etc.
                if (prefabSystem.Get(message.playerPrefabId, out Entity prefab))
                {
                    // instantiate player prefab
                    Entity player = EntityManager.Instantiate(prefab);

                    // join as left player
                    if (!pongServer.leftPlayerExists)
                    {
                        SetComponent(player, new Translation{Value=leftSpawnPosition});
                        server.JoinWorld(connectionId, player);
                        pongServer.leftPlayer = player;
#if UNITY_EDITOR
                        EntityManager.SetName(player, "Left Racket");
#endif
                    }
                    // join as right player
                    else if (!pongServer.rightPlayerExists)
                    {
                        SetComponent(player, new Translation{Value=rightSpawnPosition});
                        server.JoinWorld(connectionId, player);
                        pongServer.rightPlayer = player;
#if UNITY_EDITOR
                        EntityManager.SetName(player, "Right Racket");
#endif
                    }
                }
            }
        }
    }
}