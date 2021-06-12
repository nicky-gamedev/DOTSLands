using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Jobs;
using DOTSNET;
using System.Collections.Generic;

[ClientWorld]
[UpdateInGroup(typeof(ClientConnectedSimulationSystemGroup))]
public class SoldierRequestCatcher : SystemBase
{
    [AutoAssign] PrefabSystem prefabSystem;
    [AutoAssign] NetworkClientSystem client;
    BeginSimulationEntityCommandBufferSystem bufferSystem;
    NativeList<SpawnOwnedPrefab> messages;
    Bytes16 soldierPrefabID;
    int troopIDGen = 0;
    ulong thisPlayerRef;

    protected override void OnCreate()
    {
        base.OnCreate();
        messages = new NativeList<SpawnOwnedPrefab>();
    }

    protected override void OnStartRunning()
    {
        UnityEngine.Debug.Log("soldier start running");
        bufferSystem = Bootstrap.ClientWorld.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
        base.OnStartRunning();
        foreach (KeyValuePair<Bytes16, Entity> kvp in prefabSystem.prefabs)
        {
            if (HasComponent<PlayerTag>(kvp.Value))
            {
                soldierPrefabID = kvp.Key;
                return;
            }
        }
    }

    protected override void OnUpdate()
    {
        if(thisPlayerRef == 0)
        {
            ulong _playerRef = thisPlayerRef;
            Entities.ForEach(
                (in PlayerDataComponent player, in NetworkEntity net) =>
                {
                    if (net.owned)
                    {
                        _playerRef = net.netId;
                    }
                }).Run();
            thisPlayerRef = _playerRef;
        }

        var _messages = messages;
        var _prefabID = soldierPrefabID;
        EntityCommandBuffer ecb = bufferSystem.CreateCommandBuffer();
        troopIDGen = GetEntityQuery(ComponentType.ReadOnly<PlayerTag>()).CalculateEntityCount() + 1;
        Entities.WithAll<RequestSoldier>().ForEach(
        (Entity entity) =>
        {
            ecb.RemoveComponent(entity, typeof(RequestSoldier));
            client.Send(new SpawnOwnedPrefab { key = _prefabID, id = 0, troop = troopIDGen, playerID = thisPlayerRef});
        }).WithoutBurst().Run();
    }

    protected override void OnStopRunning()
    {
        base.OnStopRunning();
        thisPlayerRef = 0;
    }
}

[ServerWorld]
public class SpawnOwnedPrefabListener : NetworkServerMessageSystem<SpawnOwnedPrefab>
{
    [AutoAssign] PrefabSystem prefabSystem;
    protected override void OnMessage(int connectionId, SpawnOwnedPrefab message)
    {
        if (prefabSystem.Get(message.key, out Entity prefab))
        {
            Entity player = EntityManager.Instantiate(prefab);
            SetComponent(player, new Translation { Value = new float3(0, 0, 0) });
            server.Spawn(player, connectionId);
            var id = GetComponent<NetworkEntity>(player).netId;
            server.Send(connectionId, new AssignTroop { entityID = id, troopID = message.troop, playerID = message.playerID });
            foreach(int connection in server.connections.Keys)
            {
                server.Send(connection, new SpawnMeshMessage(id));
            }
        }
    }

    protected override void OnUpdate() { }

    protected override bool RequiresAuthentication() { return true; }
}

public class AssignTroopListener : NetworkClientMessageSystem<AssignTroop>
{
    ulong idToSearch;
    ulong _playerRef;
    int _troopID;
    int limitToSearch = 500;
    int searches;

    protected override void OnMessage(AssignTroop message)
    {
        _troopID = message.troopID;
        idToSearch = message.entityID;
        _playerRef = message.playerID;
        if (client.spawned.TryGetValue(idToSearch, out Entity entity))
        {
            EntityManager.SetComponentData(entity, new SoldierComponent 
            { 
                troopID =  _troopID, 
                playerRef = _playerRef
            });
            UnityEngine.Debug.Log("Soldier found, assign troop");
        }
        else
        {
            searches = 0;
        }
    }

    protected override void OnUpdate() 
    {
        if (client.spawned.TryGetValue(idToSearch, out Entity entity) && searches < limitToSearch)
        {
            UnityEngine.Debug.Log("Soldier found, assign troop");
            EntityManager.SetComponentData(entity, new SoldierComponent
            {
                troopID = _troopID,
                playerRef = _playerRef
            });
            searches = limitToSearch;
        }
        else
        {
            searches++;
        }
    }
}


public struct SpawnOwnedPrefab : NetworkMessage
{
    public ushort GetID() { return 0x2006; }
    public Bytes16 key;
    public int id;
    public int troop;
    public ulong playerID;
}

public struct AssignTroop : NetworkMessage
{
    public ushort GetID() { return 0x2007; }
    public ulong entityID;
    public int troopID;
    public ulong playerID;
}
