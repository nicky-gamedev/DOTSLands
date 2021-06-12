using DOTSNET;
using Unity.Collections;

public struct PlayerJoinMessage : NetworkMessage
{
    public Bytes16 playerPrefabId;

    public ushort GetID() { return 0x1001; }

    public PlayerJoinMessage(Bytes16 playerPrefabId)
    {
        this.playerPrefabId = playerPrefabId;
    }
}
