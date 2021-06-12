using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct SoldierComponent : IComponentData
{
    public int troopID;
    public ulong playerRef;
}
