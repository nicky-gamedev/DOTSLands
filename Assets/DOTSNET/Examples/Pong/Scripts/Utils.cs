using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace DOTSNET.Examples.Pong
{
    public static class Utils
    {
        // check collision between two colliders in world space
        public static bool CheckCollision(PhysicsCollider a, RigidTransform aRT,
                                          PhysicsCollider b, RigidTransform bRT)
        {
            if (a.IsValid && b.IsValid)
            {
                Aabb boundsA = a.Value.Value.CalculateAabb(aRT);
                Aabb boundsB = b.Value.Value.CalculateAabb(bRT);
                return boundsA.Contains(boundsB) ||
                       boundsB.Contains(boundsA) ||
                       boundsA.Overlaps(boundsB) ||
                       boundsB.Overlaps(boundsA);
            }
            return false;
        }

        // for convenience: check collision between two Entities
        public static bool CheckCollision(World world, Entity a, Entity b)
        {
            Translation aTranslation = world.EntityManager.GetComponentData<Translation>(a);
            Rotation aRotation = world.EntityManager.GetComponentData<Rotation>(a);
            RigidTransform aRT = new RigidTransform(aRotation.Value, aTranslation.Value);
            PhysicsCollider aCollider = world.EntityManager.GetComponentData<PhysicsCollider>(a);

            Translation bTranslation = world.EntityManager.GetComponentData<Translation>(b);
            Rotation bRotation = world.EntityManager.GetComponentData<Rotation>(b);
            RigidTransform bRT = new RigidTransform(bRotation.Value, bTranslation.Value);
            PhysicsCollider bCollider = world.EntityManager.GetComponentData<PhysicsCollider>(b);

            return CheckCollision(aCollider, aRT, bCollider, bRT);
        }
    }
}