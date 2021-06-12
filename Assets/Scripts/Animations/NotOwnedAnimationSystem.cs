using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using DOTSNET;
using Reese.Nav;

[ServerWorld]
public class NotOwnedAnimationSystem : NetworkServerMessageSystem<NotOwnedUpdate>
{
    protected override void OnMessage(int connectionId, NotOwnedUpdate message)
    {
        foreach (int connection in server.connections.Keys)
        {
            if (connection == connectionId) continue;

            NotOwnedUpdate messageNew = new NotOwnedUpdate
                (message.isMoving == 1, 
                new float3((float)message.x, (float)message.y, (float)message.z), 
                message.id);
            server.Send(connection, messageNew);
        }
    }

    protected override void OnUpdate() { }
    protected override bool RequiresAuthentication() { return true; }
}

public class SyncAnimationMessage : NetworkClientMessageSystem<NotOwnedUpdate>
{
    protected override void OnMessage(NotOwnedUpdate message)
    {
        if (client.spawned.TryGetValue(message.id, out Entity entity))
        {
            var input = EntityManager.GetComponentData<AnimationInputComponent>(entity);
            input.moving = message.isMoving == 1;
            input.forward = new float3((float)message.x, (float)message.y, (float)message.z);

            EntityManager.SetComponentData(entity, input);
        }
    }

    protected override void OnUpdate() { }
}

[ClientWorld]
[UpdateInGroup(typeof(ClientConnectedSimulationSystemGroup))]
[UpdateAfter(typeof(AgentSyncSystem))]
[AlwaysUpdateSystem]
public class SendAnimationToServer : SystemBase
{
    [AutoAssign] NetworkClientSystem client;
    BeginSimulationEntityCommandBufferSystem bufferSystem;
    NativeList<NotOwnedUpdate> updates;

    protected override void OnCreate()
    {
        base.OnCreate();
        updates = new NativeList<NotOwnedUpdate>(1000, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        updates.Dispose();
        base.OnDestroy();
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        bufferSystem = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var _updates = updates;
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback);

        Entities.ForEach(
            (Entity entity, 
            in NetworkEntity net, 
            in NavAgent navAgent, 
            in PlayerAgentComponent player, 
            in LocalToWorld local) =>
            {
                var animData = new AnimationInputComponent 
                { 
                    forward = local.Forward, 
                    moving = HasComponent<NavLerping>(entity) 
                };
                ecb.SetComponent(player.player, animData);
                _updates.Add(new NotOwnedUpdate(animData.moving, animData.forward, player.ID));
            }).Run();
        try
        {
            ecb.Playback(EntityManager);
        }
        catch
        {
            Debug.Log("Not possible to assign all players animations. Skipping frame...");
        }

        client.Send(_updates);
        _updates.Clear();
        ecb.Dispose();
    }
}

public struct NotOwnedUpdate : NetworkMessage
{
    public ushort GetID() { return 0x2008; }

    public byte isMoving;
    public double x;
    public double y;
    public double z;
    public ulong id;

    public NotOwnedUpdate(bool moving, float3 fwrd, ulong _id)
    {
        isMoving = moving ? (byte)1 : (byte)0;
        x = fwrd.x;
        y = fwrd.y;
        z = fwrd.z;
        id = _id;
    }
}