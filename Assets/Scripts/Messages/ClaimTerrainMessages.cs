using DOTSNET;

public struct ClaimAction : NetworkMessage
{
    public ushort GetID() { return 0x2001; }
    public ulong id;

    public ClaimAction(ulong ID)
    {
        id = ID;
    }
}

public struct ClaimComponentUpdate : NetworkMessage
{
    public ushort GetID() { return 0x2002; }
    public ulong serverTerrainBelongsTo;
    public ulong id;

    public ClaimComponentUpdate(ulong belongsTo, ulong ID)
    {
        serverTerrainBelongsTo = belongsTo;
        id = ID;
    }
}

public struct ClaimDisputingStart : NetworkMessage
{
    public ushort GetID() { return 0x2003; }
    public ulong id;
    public ulong playerID;

    public ClaimDisputingStart(ulong terrain, ulong player)
    {
        id = terrain;
        playerID = player;
    }
}

public struct ClaimDisputingStop : NetworkMessage
{
    public ushort GetID() { return 0x2004; }
    public ulong id;
    public ulong playerID;

    public ClaimDisputingStop(ulong terrain, ulong player)
    {
        id = terrain;
        playerID = player;
    }
}