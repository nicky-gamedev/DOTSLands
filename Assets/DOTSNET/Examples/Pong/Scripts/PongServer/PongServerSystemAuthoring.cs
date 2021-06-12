using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace DOTSNET.Examples.Pong
{
    public class PongServerSystemAuthoring : NetworkServerAuthoring
    {
        // ball prefab
        public NetworkEntityAuthoring ballPrefab;

        // the system
        PongServerSystem server =>
            Bootstrap.ServerWorld.GetExistingSystem<PongServerSystem>();

        // add system if Authoring is used
        public override Type GetSystemType() => typeof(PongServerSystem);

        // apply configuration
        protected override void Awake()
        {
            base.Awake();
            server.ballPrefabId = Conversion.GuidToBytes16(ballPrefab.prefabId);
        }
    }

    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class PongServerSystem : NetworkServerSystem
    {
        // dependencies
        [AutoAssign] protected NetworkServerSystem server;
        [AutoAssign] protected PrefabSystem prefabs;

        // player Entities for easier access.
        // check EntityManager.Exists first.
        public Entity leftPlayer;
        public Entity rightPlayer;
        public Entity ball;

        // for convenience
        public bool leftPlayerExists => EntityManager.Exists(leftPlayer);
        public bool rightPlayerExists => EntityManager.Exists(rightPlayer);
        public bool ballExists => EntityManager.Exists(ball);

        public Bytes16 ballPrefabId;

        void SpawnBall()
        {
            // get the ECS prefab
            if (prefabs.Get(ballPrefabId, out Entity prefab))
            {
                // instantiate
                ball = EntityManager.Instantiate(prefab);

                // spawn it on all clients, owned by no one
                server.Spawn(ball, null);
            }
            else Debug.LogError("Failed to find Ball prefab. Was it added to the PrefabSystem's spawnable prefabs list?");
        }

        protected override void OnUpdate()
        {
            // both players created?
            if (leftPlayerExists && rightPlayerExists)
            {
                // ball not created yet?
                if (!ballExists)
                {
                    // spawn the ball
                    SpawnBall();
                }
            }
        }
    }
}