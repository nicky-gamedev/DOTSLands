using Unity.Collections;

namespace DOTSNET.Examples.Pong
{
    public struct JoinWorldMessage : NetworkMessage
    {
        public Bytes16 playerPrefabId;

        public ushort GetID() { return 0x1001; }

        public JoinWorldMessage(Bytes16 playerPrefabId)
        {
            this.playerPrefabId = playerPrefabId;
        }
    }
}