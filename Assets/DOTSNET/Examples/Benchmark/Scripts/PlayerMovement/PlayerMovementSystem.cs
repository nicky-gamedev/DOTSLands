// move player locally.
// NetworkTransform component's syncDirection needs to be set CLIENT_TO_SERVER!
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace DOTSNET.Examples.Benchmark
{
    [ClientWorld]
    [UpdateInGroup(typeof(ClientConnectedSimulationSystemGroup))]
    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class PlayerMovementSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // get input
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            // get delta time
            float deltaTime = Time.DeltaTime;

            // for the local player
            Entities.ForEach((ref Translation translation,
                              in NetworkEntity networkEntity,
                              in PlayerMovementData movement) =>
            {
                // is this our player?
                if (!networkEntity.owned)
                    return;

                // move (normalize diagonal but only if >1 to respect GetAxis acceleration)
                float3 direction = new float3(horizontal, 0, vertical);
                if (math.length(direction) > 1)
                    direction = math.normalize(direction);
                translation.Value += direction * (deltaTime * movement.speed);
            })
            .Run();
        }
    }
}
