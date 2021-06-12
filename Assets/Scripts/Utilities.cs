using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using RaycastHit = Unity.Physics.RaycastHit;
using UnityEngine;
using Unity.Physics.Systems;
using DOTSNET;

public static class MiscFunctions
{
    public static RaycastHit CastRaycast(float3 from, float3 to)
    {
        BuildPhysicsWorld buildPhysicsWorld = Bootstrap.ClientWorld.GetExistingSystem<BuildPhysicsWorld>();
        CollisionWorld collisionWorld = buildPhysicsWorld.PhysicsWorld.CollisionWorld;

        RaycastInput raycastInput = new RaycastInput
        {
            Start = from,
            End = to,
            Filter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = ~0u,
                GroupIndex = 0,
            }
        };

        RaycastHit hit = new RaycastHit();

        collisionWorld.CastRay(raycastInput, out hit);
        return hit;
    }
    public static RaycastHit CastRaycast(float3 from, float3 to, CollisionFilter collisionFilter)
    {
        BuildPhysicsWorld buildPhysicsWorld = Bootstrap.ClientWorld.GetExistingSystem<BuildPhysicsWorld>();
        CollisionWorld collisionWorld = buildPhysicsWorld.PhysicsWorld.CollisionWorld;

        RaycastInput raycastInput = new RaycastInput
        {
            Start = from,
            End = to,
            Filter = collisionFilter
        };

        RaycastHit hit = new RaycastHit();
        try
        {
            collisionWorld.CastRay(raycastInput, out hit);
        }
        catch
        {
            Debug.Log("Holy cow, returning");
            return new RaycastHit();
        }
        return hit;
    }

    public static float3 ToEulerAngles(quaternion q)
    {
        float3 angles;

        // roll (x-axis rotation)
        double sinr_cosp = 2 * (q.value.w * q.value.x + q.value.y * q.value.z);
        double cosr_cosp = 1 - 2 * (q.value.x * q.value.x + q.value.y * q.value.y);
        angles.x = (float)math.atan2(sinr_cosp, cosr_cosp);

        // pitch (y-axis rotation)
        double sinp = 2 * (q.value.w * q.value.y - q.value.z * q.value.x);
        if (math.abs(sinp) >= 1)
            angles.y = (float)CopySign(math.PI / 2, sinp); // use 90 degrees if out of range
        else
            angles.y = (float)math.asin(sinp);

        // yaw (z-axis rotation)
        double siny_cosp = 2 * (q.value.w * q.value.z + q.value.x * q.value.y);
        double cosy_cosp = 1 - 2 * (q.value.y * q.value.y + q.value.z * q.value.z);
        angles.z = (float)math.atan2(siny_cosp, cosy_cosp);

        return angles;
    }

    private static double CopySign(double a, double b)
    {
        return math.abs(a) * math.sign(b);
    }
}
