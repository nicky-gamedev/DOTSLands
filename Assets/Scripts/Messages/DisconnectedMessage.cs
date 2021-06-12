using Unity.Entities;
using DOTSNET;

public struct DisconnectedMessage : NetworkMessage
{
    public ushort GetID() { return 0x2007; }

}
