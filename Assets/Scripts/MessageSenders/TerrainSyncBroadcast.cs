using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DOTSNET;

[ServerWorld]
public class TerrainSyncBroadcast : NetworkBroadcastSystem
{
    NativeMultiHashMap<int, ClaimComponentUpdate> messages;
    NativeList<ClaimComponentUpdate> messagesList;

    protected override void OnCreate()
    {
        base.OnCreate();

        messages = new NativeMultiHashMap<int, ClaimComponentUpdate>(1000, Allocator.Persistent);
        messagesList = new NativeList<ClaimComponentUpdate>(1000, Allocator.Persistent);
    }
    protected override void OnDestroy()
    {
        messagesList.Dispose();
        messages.Dispose();

        base.OnDestroy();
    }

    protected override void Broadcast()
    {
        var PlayerQuery = GetEntityQuery(ComponentType.ReadOnly<PlayerDataComponent>(),
            ComponentType.ReadOnly<NetworkEntity>()).ToComponentDataArray<NetworkEntity>(Allocator.TempJob);

        Entities.ForEach(
            (ref ClaimComponent claim) =>
            {
                bool isValid = false;
                for (int i = 0; i < PlayerQuery.Length; i++)
                {
                    isValid = claim.belongsTo == PlayerQuery[i].netId;
                    if (isValid) return;
                }
                if (!isValid && claim.belongsTo != 0)
                {
                    claim.belongsTo = 0;
                }
            }).Run();
        PlayerQuery.Dispose();

        NativeMultiHashMap<int, ClaimComponentUpdate> _messages = messages;
        Entities.ForEach(
            (ref ClaimComponent claim, in NetworkEntity net, in DynamicBuffer<NetworkObserver> observers) =>
            {
                ClaimComponentUpdate message = new ClaimComponentUpdate(claim.belongsTo, net.netId);
                for (int i = 0; i < observers.Length; i++)
                {
                    _messages.Add(observers[i], message);
                }
            }).Run();

        foreach (int connectionId in server.connections.Keys)
        {
            messagesList.Clear();
            NativeMultiHashMapIterator<int>? it = default;
            while (messages.TryIterate(connectionId, out ClaimComponentUpdate message, ref it))
            {
                messagesList.Add(message);
            }
            server.Send(connectionId, messagesList, Channel.Unreliable);
        }

        messages.Clear();
    }
}




