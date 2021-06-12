using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using System.Collections.Generic;
using DOTSNET;
using System.Linq;

[ServerWorld]
public class DisputingMessageStartClientListener : NetworkServerMessageSystem<ClaimDisputingStart>
{
    protected override void OnMessage(int connectionId, ClaimDisputingStart message)
    {
        Debug.Log("Enter trigger message received, id: " + message.id + ", connection " + connectionId);
        Bootstrap.ServerWorld.GetExistingSystem<ClocksSystem>().RegisterNewTimer(message.id, message.playerID, 5f);
    }

    protected override void OnUpdate() { }

    protected override bool RequiresAuthentication() { return true; }
}

[ServerWorld]
public class DisputingMessageStopClientListener : NetworkServerMessageSystem<ClaimDisputingStop>
{
    protected override void OnMessage(int connectionId, ClaimDisputingStop message)
    {
        Debug.Log("Exit trigger message received, id: " + message.id + ", connection " + connectionId);
        Bootstrap.ServerWorld.GetExistingSystem<ClocksSystem>().StopATimer(message.id);
    }

    protected override void OnUpdate() { }

    protected override bool RequiresAuthentication() { return true; }
}

[ServerWorld]
[UpdateInGroup(typeof(ServerActiveSimulationSystemGroup))]
public class ClocksSystem : SystemBase
{
    Dictionary<ulong, float> timers;
    Dictionary<ulong, ulong> terrainPlayer;
    [AutoAssign] private NetworkServerSystem server;

    protected override void OnStartRunning()
    {
        timers = new Dictionary<ulong, float>();
        terrainPlayer = new Dictionary<ulong, ulong>();
    }

    protected override void OnUpdate()
    {
        foreach(ulong terrainID in timers.Keys.ToList())
        {
            if(timers[terrainID] >= 0)
            {
                timers[terrainID] -= Time.DeltaTime;
            }
            else
            {
                if(server.spawned.TryGetValue(terrainID, out Entity entity))
                {
                    var cc = EntityManager.GetComponentData<ClaimComponent>(entity);
                    cc.belongsTo = terrainPlayer[terrainID];
                    EntityManager.SetComponentData(entity, cc);
                    Debug.Log("Sucess, terrain " + terrainID + " claimed by terrain player " + terrainPlayer[terrainID]);
                    StopATimer(terrainID);
                }
            }
        }
    }

    public void RegisterNewTimer(ulong terrainID,ulong player, float time)
    {
        if (timers.ContainsKey(terrainID))
        {
            if(terrainPlayer[terrainID] == player)
            {
                Debug.LogWarning("Player called twice, but the clock is already registered");
                return;
            }
            Debug.Log("Another player on the same trigger, stopping operation");
            StopATimer(terrainID);
            return;
        }

        timers.Add(terrainID, time);
        terrainPlayer.Add(terrainID, player);
        Debug.Log("Registered new timer for " + terrainID + ", claimed by player " + player);
    }

    public void StopATimer(ulong terrainID)
    {
        if (timers.ContainsKey(terrainID))
        {
            timers.Remove(terrainID);
            terrainPlayer.Remove(terrainID);
            Debug.Log("Stopped a timer for " + terrainID);
        }
        else
        {
            Debug.LogWarning("Tried to stop a clock that don't exist.");
        }
    }
}
