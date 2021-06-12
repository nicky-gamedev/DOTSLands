using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace DOTSNET.Examples.Pong
{
    public class RacketMovementClientSystemAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        // add system if Authoring is used
        public Type GetSystemType() => typeof(RacketMovementClientSystem);
    }

    [ClientWorld]
    [UpdateInGroup(typeof(ClientConnectedSimulationSystemGroup))]
    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class RacketMovementClientSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // get input
            float vertical = Input.GetAxisRaw("Vertical");

            // get delta time
            float deltaTime = Time.DeltaTime;

            // for the local player
            Entities.ForEach((ref Translation translation,
                              in Entity entity,
                              in NetworkEntity networkEntity,
                              in RacketMovementData movement) =>
            {
                // is this our player?
                if (!networkEntity.owned)
                    return;

                // move within bounds
                float step = vertical * movement.speed * deltaTime;
                float z = translation.Value.z;
                translation.Value.z = math.clamp(z + step, movement.minZ, movement.maxZ);
            })
            .Run();
        }
    }
}