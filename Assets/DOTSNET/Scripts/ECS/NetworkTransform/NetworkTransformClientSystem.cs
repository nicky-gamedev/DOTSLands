// Broadcasts position+rotation from client to server.
//
// Benchmark: 75k Entities, max distance, only 1 player owned object
//
//    ____________________|_System_Time_|
//    Run() without Burst |     11 ms   |
//    Run() with    Burst |   0.33 ms   |
//
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace DOTSNET
{
    // note: [AlwaysUpdateSystem] isn't needed because we should only broadcast
    //       if there are entities around.
    [ClientWorld]
    [UpdateInGroup(typeof(ClientConnectedSimulationSystemGroup))]
    // use SelectiveAuthoring to create/inherit it selectively
    [DisableAutoCreation]
    public class NetworkTransformClientSystem : SystemBase
    {
        // dependencies
        [AutoAssign] protected NetworkClientSystem client;

        // send state to server every 100ms
        // (modified by NetworkServerSystemAuthoring component)
        public float interval = 0.1f;
        double lastSendTime;

        // NativeList so we can run most of it with Burst enabled
        NativeList<TransformMessage> messages;

        protected override void OnCreate()
        {
            base.OnCreate();
            // create with small capacity. most games don't have >10 player
            // owned objects
            messages = new NativeList<TransformMessage>(10, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            messages.Dispose();
            base.OnDestroy();
        }

        void Send()
        {
            // run with Burst
            // NOTE: if this is too slow, we could add a Owned tag for local
            //       player owned objects later and iterate only those.
            //       0.33ms is nothing to worry about yet though.
            NativeList<TransformMessage> _messages = messages;
            Entities.ForEach((in Entity entity,
                              in Translation translation,
                              in Rotation rotation,
                              in NetworkEntity networkEntity,
                              in NetworkTransform networkTransform) =>
            {
                // only if client authority
                if (networkTransform.syncDirection != SyncDirection.CLIENT_TO_SERVER)
                    return;

                // only for objects owned by this connection
                if (!networkEntity.owned)
                    return;

                // create the message
                TransformMessage message = new TransformMessage(
                    networkEntity.netId,
                    translation.Value,
                    rotation.Value
                );

                // add to messages and send afterwards without burst
                _messages.Add(message);
            })
            .Run();

            // send after the ForEach. this way we can run ForEach with Burst(!)
            client.Send(_messages);
            messages.Clear();
        }

        // update sends state every couple of seconds
        protected override void OnUpdate()
        {
            if (Time.ElapsedTime >= lastSendTime + interval)
            {
                Send();
                lastSendTime = Time.ElapsedTime;
            }
        }
    }
}
