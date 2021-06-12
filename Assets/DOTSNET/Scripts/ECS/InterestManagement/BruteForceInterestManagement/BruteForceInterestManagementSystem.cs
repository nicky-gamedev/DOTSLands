// DOTS is fast. this is a simple brute force system for now.
//
// Benchmark:  10k Entities, max distance, interval=0, memory transport
//
//    ____________________|_System_Time_|
//    Run() without Burst |  339 ms     |
//    Run() with    Burst |    5.89 ms  |
//
// Job:
//   See 'bruteforceinterestmanagement_job_notworking_yet' branch.
//   Following the 'fire & forget' rule for Jobs, it's not a good idea to
//   schedule a Job here.
//   The code gets way more complicated and we get strange race conditions.
//   The Job wasn't that much faster either. System time goes from 5ms to 1.5-3ms.
//   The Job makes this way harder to test as well.
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DOTSNET
{
    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(ServerActiveSimulationSystemGroup))]
    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class BruteForceInterestManagementSystem : InterestManagementSystem
    {
        // visibility radius
        // modified by Authoring component!
        public float visibilityRadius = float.MaxValue;

        // don't update every tick. update every so often.
        // note: can't used FixedRateUtils because they only work for groups
        public float updateInterval = 1;
        double lastUpdateTime;

        // owned entities per connection cache usable from Jobs
        NativeList<int> connections;
        NativeMultiHashMap<int, float3> ownedPerConnection;

        protected override void OnCreate()
        {
            // call base because it might be implemented.
            base.OnCreate();

            // create collections
            connections = new NativeList<int>(1000, Allocator.Persistent);
            ownedPerConnection = new NativeMultiHashMap<int, float3>(1000, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            // dispose safely with Dependency in case Job is still running
            connections.Dispose();
            ownedPerConnection.Dispose();

            // call base because it might be implemented.
            base.OnDestroy();
        }

        void RebuildObservers()
        {
            // for each NetworkEntity, we need to check if it's visible from
            // ANY of the player's entities. not just the main player.
            //
            // consider a MOBA game where a player might place a watchtower at
            // the other end of the map:
            // * if we check visibility only to the main player, then the watch-
            //   tower would not see anything
            // * if we check visibility to all player objects, both the watch-
            //   tower and the main player object would see enemies

            // copy server.connections.ownedEntities into a MultiHashMap usable
            // from Jobs
            // -> we need to copy it anyway, so it's okay if server.connections
            //    is not a NativeCollection
            ownedPerConnection.Clear();
            connections.Clear();
            foreach (KeyValuePair<int, ConnectionState> kvp in server.connections)
            {
                connections.Add(kvp.Key);
                foreach (Entity entity in kvp.Value.ownedEntities)
                {
                    ownedPerConnection.Add(kvp.Key, GetComponent<Translation>(entity).Value);
                }
            }

            // Rebuild observers and store result in Rebuild buffer
            float _visibilityRadius = visibilityRadius;
            NativeList<int> _connections = connections;
            NativeMultiHashMap<int, float3> _ownedPerConnection = ownedPerConnection;
            Entities.ForEach((DynamicBuffer<NetworkObserver> observers,
                              DynamicBuffer<RebuildNetworkObserver> rebuild,
                              in Translation translation,
                              in NetworkEntity networkEntity) =>
            {
                // clear previous rebuild first
                rebuild.Clear();

                // it would be enough to check distance with each owned entity
                // from each server connection's ownedEntities.
                // BUT we would have to convert those to NativeLists and
                // NativeMultiMaps, with the latter being difficult to iterate.
                //
                // For now, let's brute force check each Entity with each other
                // Entity.
                for (int i = 0; i < _connections.Length; ++i)
                {
                    int connectionId = _connections[i];

                    // is it visible to ANY of the connection's owned entities?
                    NativeMultiHashMapIterator<int>? it = default;
                    while (_ownedPerConnection.TryIterate(connectionId, out float3 position, ref it))
                    {
                        // check visibility
                        float distance = math.distance(translation.Value, position);
                        if (distance <= _visibilityRadius)
                        {
                            // add and stop here. they all have the same
                            // connectionId anyway
                            // (we need Contains check because rebuild should
                            //  act like a HashSet)
                            if (!rebuild.Contains(connectionId))
                            {
                                rebuild.Add(connectionId);
                            }
                            break;
                        }
                    }
                }
            })
            .Run();
        }

        void RemoveOldObservers()
        {
            // remove old observers and add unspawn message
            NativeMultiHashMap<int, UnspawnMessage> _unspawnMessages = unspawnMessages;
            Entities.ForEach((DynamicBuffer<NetworkObserver> observers,
                              DynamicBuffer<RebuildNetworkObserver> rebuild,
                              in NetworkEntity networkEntity) =>
            {
                // DynamicBuffer foreach allocates. use for.
                for (int i = 0; i < observers.Length; ++i)
                {
                    int connectionId = observers[i];
                    if (!rebuild.Contains(connectionId))
                    {
                        //Debug.LogWarning(EntityManager.GetName(entity) + " old observer found with connectionId=" + connectionId);

                        // add to unspawn messages so we can run ForEach with
                        // Burst(!) and send it later.

                        UnspawnMessage message = new UnspawnMessage(networkEntity.netId);
                        _unspawnMessages.Add(connectionId, message);

                        // remove it from the observers buffer
                        observers.RemoveAt(i);
                        --i;
                    }
                }
            })
            .Run();
        }

        void AddNewObservers()
        {
            // add new observers and add spawn messages
            NativeMultiHashMap<int, SpawnMessage> _spawnMessages = spawnMessages;
            Entities.ForEach((DynamicBuffer<NetworkObserver> observers,
                              DynamicBuffer<RebuildNetworkObserver> rebuild,
                              in NetworkEntity networkEntity,
                              in Translation translation,
                              in Rotation rotation) =>
            {
                // DynamicBuffer foreach allocates. use for.
                // (foreach also gives "Invalid IL Code")
                for (int i = 0; i < rebuild.Length; ++i)
                {
                    int connectionId = rebuild[i];
                    if (!observers.Contains(connectionId))
                    {
                        //Debug.LogWarning(EntityManager.GetName(entity) + " new observer found with connectionId=" + connectionId);

                        // is the entity owned by the observer connection?
                        bool owned = networkEntity.connectionId == connectionId;

                        // add to spawn messages so we can run ForEach with
                        // Burst(!) and send it later.
                        SpawnMessage message = new SpawnMessage(
                            networkEntity.prefabId,
                            networkEntity.netId,
                            (byte)(owned ? 1 : 0),
                            translation.Value,
                            rotation.Value
                        );
                        _spawnMessages.Add(connectionId, message);

                        // add it to the observers buffer
                        observers.Add(connectionId);
                    }
                }
            })
            .Run();
        }

        public override void RebuildAll()
        {
            // ForEach calls split into separate functions to avoid a ECS bug
            // where the Editor crashes when using NativeMultiMap in a function
            // that has multiple ForEach calls.
            // (see also: interestmanagement_messagebuffers_EDITORCRASH branch)
            RebuildObservers();
            RemoveOldObservers();
            AddNewObservers();

            // send all un/spawn messages
            FlushMessages();

            // RebuildAll can be called from the outside, so we should set the
            // lastUpdateTime in here instead of in Update.
            // this way we avoid updating if someone from the outside recently
            // did a rebuild anyway (e.g. in OnServerDisconnect).
            lastUpdateTime = Time.ElapsedTime;
        }

        // update rebuilds every couple of seconds
        protected override void OnUpdate()
        {
            if (Time.ElapsedTime >= lastUpdateTime + updateInterval)
            {
                RebuildAll();
            }
        }
    }
}
