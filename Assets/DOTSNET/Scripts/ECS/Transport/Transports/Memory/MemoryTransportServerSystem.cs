// MemoryTransport is useful for:
// * Unit tests
// * Benchmarks where DOTS isn't limited by socket throughput
// * WebGL demos
// * Single player mode
// * etc.
using System;
using System.Collections.Generic;
using Unity.Entities;

namespace DOTSNET.MemoryTransport
{
    [ServerWorld]
    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class MemoryTransportServerSystem : TransportServerSystem
    {
        // can't use [AutoAssign] because clientTransport is in another world
        public MemoryTransportClientSystem clientTransport;

        bool active;
        internal Queue<Message> incoming = new Queue<Message>();

        public override bool Available() => true;
        // 16MB is a reasonable max packet size. we don't want to allocate
        // int max = 2GB for buffers each time.
        public override int GetMaxPacketSize() => 16 * 1024 * 1024;
        public override bool IsActive() => active;
        public override void Start() { active = true; }
        public override bool Send(int connectionId, ArraySegment<byte> segment, Channel channel)
        {
            // only if server is running and client is connected
            if (active && clientTransport.IsConnected())
            {
                // copy segment data because it's only valid until return
                byte[] data = new byte[segment.Count];
                Array.Copy(segment.Array, segment.Offset, data, 0, segment.Count);

                // add client data message
                clientTransport.incoming.Enqueue(new Message(0, EventType.Data, data));
                return true;
            }
            return false;
        }
        public override bool Disconnect(int connectionId)
        {
            // only disconnect if it was the 0 client
            if (connectionId == 0)
            {
                // add client disconnected message
                clientTransport.incoming.Enqueue(new Message(0, EventType.Disconnected, null));

                // server needs to know it too
                incoming.Enqueue(new Message(0, EventType.Disconnected, null));
                return true;
            }
            return false;
        }
        public override string GetAddress(int connectionId) => string.Empty;
        public override void Stop()
        {
            // clear all pending messages that we may have received.
            // over the wire, we wouldn't receive any more pending messages
            // ether after calling stop.
            incoming.Clear();

            // add client disconnected message
            clientTransport.incoming.Enqueue(new Message(0, EventType.Disconnected, null));

            // add server disconnected message
            incoming.Enqueue(new Message(0, EventType.Disconnected, null));

            // not active anymore
            active = false;
        }

        protected override void OnStartRunning()
        {
            // sometimes we use MemoryTransport in host mode for bench marks etc
            // so in that case, find server transport if the client world exists
            // (it won't exist during unit tests)
            if (Bootstrap.ClientWorld != null)
                clientTransport = Bootstrap.ClientWorld.GetExistingSystem<MemoryTransportClientSystem>();
        }

        // ECS
        // NOTE: we DO NOT call all the events directly. instead we use a queue
        //       and only call them in OnUpdate. this is what we do with regular
        //       transports too, and this way the tests behave exactly the same!
        protected override void OnUpdate()
        {
            while (incoming.Count > 0)
            {
                Message message = incoming.Dequeue();
                switch (message.eventType)
                {
                    case EventType.Connected:
                        //Debug.Log("MemoryTransport Server Message: Connected");
                        OnConnected(message.connectionId);
                        break;
                    case EventType.Data:
                        //Debug.Log("MemoryTransport Server Message: Data: " + BitConverter.ToString(message.data));
                        OnData(message.connectionId, new ArraySegment<byte>(message.data));
                        break;
                    case EventType.Disconnected:
                        //Debug.Log("MemoryTransport Server Message: Disconnected");
                        OnDisconnected(message.connectionId);
                        break;
                }
            }
        }
    }
}