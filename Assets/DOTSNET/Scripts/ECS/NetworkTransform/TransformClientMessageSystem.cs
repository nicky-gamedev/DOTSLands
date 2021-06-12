// Applies the TransformMessage to the Entity.
// There is no interpolation yet, only the bare minimum.
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace DOTSNET
{
    // use SelectiveAuthoring to create/inherit it selectively
    [DisableAutoCreation]
    public class TransformClientMessageSystem : NetworkClientMessageSystem<TransformMessage>
    {
        // cache new messages <netId, message> to apply all at once in OnUpdate.
        // finding the Entity with netId and calling SetComponent for one Entity
        // in OnMessage 10k times would be very slow.
        // a ForEach query is faster, it can use Burst(!) and it could be a Job.
        NativeHashMap<ulong, TransformMessage> messages;

        protected override void OnCreate()
        {
            // call base because it might be implemented.
            base.OnCreate();

            // create messages HashMap
            messages = new NativeHashMap<ulong, TransformMessage>(1000, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            // dispose with Dependency in case it's used in a Job
            messages.Dispose(Dependency);

            // call base because it might be implemented.
            base.OnDestroy();
        }

        protected override void OnMessage(TransformMessage message)
        {
            // store in messages
            // note: we might overwrite the previous NetworkTransform, but
            //       that's fine since we don't send deltas and we only care
            //       about the latest position/rotation.
            //       so this can even avoid some computations.
            messages[message.netId] = message;
        }

        protected override void OnUpdate()
        {
            // copy messages to local variable so we can use Burst
            NativeHashMap<ulong, TransformMessage> _messages = messages;

            // we assume large amounts of entities, so we go through all of them
            // and apply their NetworkTransform message (if any).
            Entities.ForEach((ref Translation translation,
                              ref Rotation rotation,
                              in NetworkEntity networkEntity) =>
            {
                // do we have a message for this netId?
                if (_messages.ContainsKey(networkEntity.netId))
                {
                    TransformMessage message = _messages[networkEntity.netId];
                    translation.Value = message.position;
                    rotation.Value = message.rotation;
                }
            })
            // DO NOT Schedule()!
            // The time it takes to start the job is way too noticeable for
            // position updates on clients. Try to run the Pong example with
            // server as build and client as build. It's way too noticeable.
            .Run();

            // clear messages
            messages.Clear();
        }
    }
}
