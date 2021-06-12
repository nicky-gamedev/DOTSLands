using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Physics.Stateful;
using Unity.Entities;
using Unity.Jobs;
using DOTSNET;
using Unity.Collections;

[ClientWorld]
[UpdateInGroup(typeof(ClientConnectedSimulationSystemGroup))]
public class PlayerTriggerEventSystem : SystemBase
{
    private EntityQueryMask m_NonTriggerMask;
    [AutoAssign] private NetworkClientSystem client;

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        m_NonTriggerMask = EntityManager.GetEntityQueryMask(
                GetEntityQuery(new EntityQueryDesc
                {
                    None = new ComponentType[]
                    {
                        typeof(StatefulTriggerEvent)
                    }
                })
            );
    }
    protected override void OnUpdate()
    {
        var nonTriggerMask = m_NonTriggerMask;


        var em = EntityManager;

        Entities.ForEach(
            (Entity e, ref DynamicBuffer<StatefulTriggerEvent> triggerEventBuffer, in PlayerTag player) => 
            {
                for (int i = 0; i < triggerEventBuffer.Length; i++)
                {
                    var triggerEvent = triggerEventBuffer[i];
                    var otherEntity = triggerEvent.GetOtherEntity(e);

                    if (triggerEvent.State == EventOverlapState.Stay)
                    {
                        continue;
                    }

                    if(triggerEvent.State == EventOverlapState.Enter)
                    {
                        if (em.HasComponent(e, ComponentType.ReadOnly<PlayerTag>()))
                        {
                            bool enter = em.GetComponentData<ClaimComponent>(otherEntity).belongsTo == 0 && em.GetComponentData<NetworkEntity>(e).owned;
                            if (enter)
                            {
                                var startMessage = new ClaimDisputingStart();
                                startMessage.id = em.GetComponentData<NetworkEntity>(otherEntity).netId;
                                startMessage.playerID = em.GetComponentData<SoldierComponent>(e).playerRef;
                                client.Send(startMessage);
                            }
                        }
                        else
                        {
                            bool enter = em.GetComponentData<ClaimComponent>(e).belongsTo == 0 && em.GetComponentData<NetworkEntity>(otherEntity).owned;
                            if (enter)
                            {
                                var startMessage = new ClaimDisputingStart();
                                startMessage.id = em.GetComponentData<NetworkEntity>(e).netId;
                                startMessage.playerID = em.GetComponentData<SoldierComponent>(e).playerRef;
                                client.Send(startMessage);
                            }
                        }
                    }

                    if(triggerEvent.State == EventOverlapState.Exit)
                    {
                        if (EntityManager.HasComponent(e, ComponentType.ReadOnly<PlayerTag>()))
                        {
                            bool exit = em.GetComponentData<ClaimComponent>(otherEntity).belongsTo == 0 && em.GetComponentData<NetworkEntity>(e).owned;
                            if (exit)
                            {
                                var exitMessage = new ClaimDisputingStop();
                                exitMessage.id = em.GetComponentData<NetworkEntity>(otherEntity).netId;
                                exitMessage.playerID = em.GetComponentData<SoldierComponent>(e).playerRef;
                                client.Send(exitMessage);
                            }
                        }
                        else
                        {
                            bool exit = em.GetComponentData<ClaimComponent>(e).belongsTo == 0 && em.GetComponentData<NetworkEntity>(otherEntity).owned;
                            if (exit)
                            {
                                var exitMessage = new ClaimDisputingStop();
                                exitMessage.id = em.GetComponentData<NetworkEntity>(e).netId;
                                exitMessage.playerID = em.GetComponentData<SoldierComponent>(e).playerRef;
                                client.Send(exitMessage);
                            }
                        }
                    }
                }
            }).WithoutBurst().Run();
    }
}