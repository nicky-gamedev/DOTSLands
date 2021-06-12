using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct SquadShapeParentTag : IComponentData
{
    public bool posUpdated;
}
