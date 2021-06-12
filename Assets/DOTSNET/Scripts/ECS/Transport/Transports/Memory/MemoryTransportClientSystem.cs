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
    [ClientWorld]
    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class MemoryTransportClientSystem : TransportClientSystem
    {
        bool connected;
        internal Queue<Message> incoming = new Queue<Message>();

        // can't use [AutoAssign] because clientTransport is in another world
        public MemoryTransportServerSystem serverTransport;

        public override bool Available() => true;
        // 16MB is a reasonable max packet size. we don't want to allocate
        // int max = 2GB for buffers each time.
        public override int GetMaxPacketSize() => 16 * 1024 * 1024;
        public override bool IsConnected() => connected;
        public override void Connect(string address)
        {
            // only if server is running
            if (serverTransport.IsActive())
            {
                // add server connected message
                serverTransport.incoming.Enqueue(new Message(0, EventType.Connected, null));

                // add client connected message
                incoming.Enqueue(new Message(0, EventType.Connected, null));

                connected = true;
            }
        }
        public override bool Send(ArraySegment<byte> segment, Channel channel)
        {
            // only  if client connected
            if (connected)
            {
                // copy segment data because it's only valid until return
                byte[] data = new byte[segment.Count];
                Array.Copy(segment.Array, segment.Offset, data, 0, segment.Count);

                // add server data message
                serverTransport.incoming.Enqueue(new Message(0, EventType.Data, data));
                return true;
            }
            return false;
        }
        public override void Disconnect()
        {
            // only  if client connected
            if (connected)
            {
                // clear all pending messages that we may have received.
                // over the wire, we wouldn't receive any more pending messages
                // ether after calling disconnect.
                incoming.Clear();

                // add server disconnected message
                serverTransport.incoming.Enqueue(new Message(0, EventType.Disconnected, null));

                // add client disconnected message
                incoming.Enqueue(new Message(0, EventType.Disconnected, null));

                // not connected anymore
                connected = false;
            }
        }

        protected override void OnStartRunning()
        {
            // sometimes we use MemoryTransport in host mode for bench marks etc
            // so in that case, find server transport if the server world exists
            // (it won't exist during unit tests)
            if (Bootstrap.ServerWorld != null)
                serverTransport = Bootstrap.ServerWorld.GetExistingSystem<MemoryTransportServerSystem>();
        }

        // ECS
        // NOTE: we DO NOT call all the events directly. instead we use a queue
        //       and only call them in OnUpdate. this is what we do with regular
        //       transports too, and this way the tests behave exactly the same!
        protected override void OnUpdate()
        {
            // note: process even if not connected because when calling
            // Disconnect, we add a Disconnected event which still needs to be
            // processed here.
            while (incoming.Count > 0)
            {
                Message message = incoming.Dequeue();
                switch (message.eventType)
                {
                    case EventType.Connected:
                        //Debug.Log("MemoryTransport Client Message: Connected");
                        OnConnected();
                        break;
                    case EventType.Data:
                        //Debug.Log("MemoryTransport Client Message: Data: " + BitConverter.ToString(message.data));
                        OnData(new ArraySegment<byte>(message.data));
                        break;
                    case EventType.Disconnected:
                        //Debug.Log("MemoryTransport Client Message: Disconnected");
                        OnDisconnected();
                        break;
                }
            }
        }
    }
}