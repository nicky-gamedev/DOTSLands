using DOTSNET;
using Unity.Collections;

public struct SpawnMeshMessage : NetworkMessage
{
    public ushort GetID() { return 0x2005; }
    public ulong id;

    public SpawnMeshMessage(ulong playerID)
    {
        id = playerID;
    }
}
