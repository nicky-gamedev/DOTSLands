using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using UnityEngine;

namespace DOTSNET.Examples.Physics
{
    public class MovementClientSystemAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        public Type GetSystemType() => typeof(MovementClientSystem);
    }

    [ClientWorld]
    [UpdateInGroup(typeof(ClientConnectedSimulationSystemGroup))]
    [DisableAutoCreation]
    public class MovementClientSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // get input
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            float3 direction = new float3(horizontal, 0, vertical);

            // normalize diagonal but only if >1 to respect GetAxis acceleration
            if (math.length(direction) > 1)
                direction = math.normalize(direction);

            // foreach
            Entities.ForEach((ref Translation translation,
                              ref PhysicsVelocity velocity,
                              in NetworkEntity networkEntity,
                              in MovementComponent movement,
                              in PhysicsMass mass) =>
            {
                // only for our own player
                if (!networkEntity.owned)
                    return;

                // dynamic body + impulse works
                velocity.ApplyLinearImpulse(mass, direction * movement.force);

                // force y=0 even when players collider or spawn inside each
                // other.
                translation.Value.y = 0;
            })
            .Run();
        }
    }
}
