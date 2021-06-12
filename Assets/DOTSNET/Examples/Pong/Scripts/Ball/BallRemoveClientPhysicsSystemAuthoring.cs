using System;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;

namespace DOTSNET.Examples.Pong
{
    public class BallRemoveClientPhysicsSystemAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        // add system if Authoring is used
        public Type GetSystemType() => typeof(BallRemoveClientPhysicsSystem);
    }

    [ClientWorld]
    [UpdateInGroup(typeof(ClientConnectedSimulationSystemGroup))]
    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class BallRemoveClientPhysicsSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // remove physics components from from spheres on the server,
            // so that we can apply NetworkTransform synchronization.
            Entities.ForEach((in Entity entity, in BallTag ball) =>
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