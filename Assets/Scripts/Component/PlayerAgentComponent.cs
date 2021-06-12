using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct PlayerAgentComponent : IComponentData
{
    public Entity player;
    public ulong ID;

    public bool destroyMe;
}
