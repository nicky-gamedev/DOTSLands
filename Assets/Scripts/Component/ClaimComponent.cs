using Unity.Entities;
using Unity.Collections;

[GenerateAuthoringComponent]
public struct ClaimComponent : IComponentData
{
    public float timeToConquest;
    public ulong belongsTo;
}
