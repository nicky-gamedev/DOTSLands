using UnityEngine;
using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Reese.Nav;
using DOTSNET;

[ClientWorld]
[UpdateInGroup(typeof(ClientConnectedSimulationSystemGroup))]
[UpdateAfter(typeof(TroopPathfindingSystem))]
public class TroopShapeRaycastSystem : SystemBase
{
    GameObject center;
    protected override void OnUpdate()
    {
        #region Checks
        if (center == null) center = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        if (!Input.GetMouseButtonDown(1)) return;
        #endregion

        #region Getting Data
        NativeList<Entity> soldiers = new NativeList<Entity>(1000, Allocator.Temp);

        Entities.ForEach(
            (Entity entity, in SelectedTroopComponent selected) => 
            {
                soldiers.Add(entity);
            }).Run();

        CollisionFilter filter = new CollisionFilter
        {
            BelongsTo = 2,
            CollidesWith = ~0u,
            GroupIndex = 0
        };

        var screenPointToRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        Unity.Physics.RaycastHit hit; 
        hit = MiscFunctions.CastRaycast(screenPointToRay.origin, screenPointToRay.origin + screenPointToRay.direction * 200, filter);
        var test = GetEntityQuery(typeof(SquadShapeParentTag)).GetSingletonEntity();

        float3 shapePos = hit.Position;
        Debug.Log(shapePos);
        #endregion

        #region Update Position
        var t = GetComponent<Translation>(test);
        t.Value = shapePos;
        #endregion

        #region Update bool
        var tag = GetComponent<SquadShapeParentTag>(test);
        tag.posUpdated = true;
        #endregion

        #region Calculating Center
        float3 avg = new float3();
        foreach (var item in soldiers)
        {
            avg += GetComponent<LocalToWorld>(item).Position;
        }
        avg /= soldiers.Length;
        avg.y = 0;

        #endregion

        #region Debugging
        if (float.IsNaN(avg.x))
            center.transform.position = float3.zero;
        else center.transform.position = avg;
        #endregion

        #region Set Rotation
        var rotation = GetComponent<Rotation>(test);
        rotation.Value = quaternion.LookRotation(avg - shapePos, new float3(0, 1, 0));
        #endregion

        #region Set Components
        SetComponent(test, t);
        SetComponent(test, tag);
        SetComponent(test, rotation);

        #endregion
    }
}
