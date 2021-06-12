using Unity.Entities;
using UnityEngine;
using DOTSNET;
using Unity.Collections;
using Unity.Rendering;
using Unity.Physics.Stateful;
using Unity.Jobs;
using System.Collections.Generic;
using System.Runtime.InteropServices;

[ClientWorld]
[UpdateInGroup(typeof(ClientConnectedSimulationSystemGroup))]
public class ChangeTerrainColorSystem : SystemBase
{
    NativeList<Color> colorArray;
    NetworkClientSystem client;

    protected override void OnCreate()
    {
        base.OnCreate();
        colorArray = new NativeList<Color>(Allocator.Persistent);
        colorArray.Add(new Color(1f, 1f, 1f, 0.5f));
        colorArray.Add(Color.blue);
        colorArray.Add(Color.red);
        colorArray.Add(Color.yellow);
        colorArray.Add(Color.magenta);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        colorArray.Dispose();
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        client = World.GetExistingSystem<NetworkClientSystem>();
    }

    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var query = GetEntityQuery(ComponentType.ReadOnly<PlayerDataComponent>()).ToEntityArray(Allocator.TempJob);
        NativeHashMap<ulong, int> playerToInt = new NativeHashMap<ulong, int>(1000, Allocator.Temp);

        foreach (Entity e in query)
        {
            var id = GetComponent<NetworkEntity>(e).netId;
            if (playerToInt.ContainsKey(id)) continue;
            playerToInt.Add(id, playerToInt.Count() + 1);
        }
        query.Dispose();

        
        Entities.ForEach(
            (RenderMesh render, int entityInQueryIndex, in ClaimComponent claim, in DynamicBuffer<StatefulTriggerEvent> triggerEvent, in Entity entity) =>
            {
                if (claim.belongsTo == 0) return;

                var color = render.material.GetColor("_Color");
                if (color == colorArray[playerToInt[claim.belongsTo]]) return;

                var mat = new Material(render.material);
                mat.SetColor("_Color", colorArray[playerToInt[claim.belongsTo]]);
                render.material = mat;

                ecb.SetSharedComponent(entity, render);
            }).WithoutBurst().Run();

        ecb.Playback(EntityManager);
        ecb.Dispose();
        playerToInt.Dispose();
    }
}