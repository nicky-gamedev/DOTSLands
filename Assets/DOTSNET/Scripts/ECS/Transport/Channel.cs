// Some transports support different channels for message sending.
// To keep it simple, we define Reliable and Unreliable.
// Transports that only support one channel (like TCP) should ignore the channel
// parameter.
namespace DOTSNET
{
    public enum Channel : byte
    {
        // Reliable is more costly, but the message is definitely delivered.
        // Use this for spawn messages etc.
        Reliable = 0,
        // Unreliable is faster, but won't try to redeliver a message.
        // Use this for position updates etc.
        Unreliable = 1
    }
}