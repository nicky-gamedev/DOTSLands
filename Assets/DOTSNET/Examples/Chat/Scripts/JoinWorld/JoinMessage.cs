using Unity.Collections;

namespace DOTSNET.Examples.Chat
{
    public struct JoinMessage : NetworkMessage
    {
        public FixedString32 name;

        public ushort GetID() { return 0x1001; }

        public JoinMessage(FixedString32 name)
        {
            this.name = name;
        }
    }
}