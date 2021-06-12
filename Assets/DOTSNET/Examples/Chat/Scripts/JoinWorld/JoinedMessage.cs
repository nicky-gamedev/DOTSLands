namespace DOTSNET.Examples.Chat
{
    public struct JoinedMessage : NetworkMessage
    {
        public ushort GetID() { return 0x1002; }
    }
}