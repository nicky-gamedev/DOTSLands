using Unity.Collections;

namespace DOTSNET.Examples.Chat
{
    public struct ChatMessage : NetworkMessage
    {
        public FixedString32 sender;
        public FixedString128 text;

        public ushort GetID() { return 0x1003; }

        public ChatMessage(FixedString32 sender, FixedString128 text)
        {
            this.sender = sender;
            this.text = text;
        }
    }
}