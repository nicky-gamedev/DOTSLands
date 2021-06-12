using System;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;

namespace DOTSNET.Examples.Physics
{
    public class MovementServerSystemAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        public Type GetSystemType() => typeof(MovementServerSystem);
    }

    [ServerWorld]
    [UpdateInGroup(typeof(ServerActiveSimulationSystemGroup))]
    [DisableAutoCreation]
    public class MovementServerSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // remove physics components from from spheres on the server,
            // so that we can apply NetworkTransform synchronization.
            Entities.ForEach((in Entity entity, in MovementComponent movement) =>
            {
                EntityManager.RemoveComponent<PhysicsCollider>(entity);
                EntityManager.RemoveComponent<PhysicsDamping>(entity);
                EntityManager.RemoveComponent<PhysicsGravityFactor>(entity);
                EntityManager.RemoveComponent<PhysicsMass>(entity);
                EntityManager.RemoveComponent<PhysicsVelocity>(entity);
            })
            .WithStructuralChanges()
            .Run();
        }
    }
}
