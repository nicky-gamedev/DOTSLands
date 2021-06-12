// MemoryTransport is useful for:
// * Unit tests
// * Benchmarks where DOTS isn't limited by socket throughput
// * WebGL demos
// * Single player mode
// * etc.

namespace DOTSNET.MemoryTransport
{
    public enum EventType { Connected, Data, Disconnected }
    public struct Message
    {
        public int connectionId;
        public EventType eventType;
        public byte[] data;
        public Message(int connectionId, EventType eventType, byte[] data)
        {
            this.connectionId = connectionId;
            this.eventType = eventType;
            this.data = data;
        }
    }
}