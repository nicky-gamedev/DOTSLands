using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
[GenerateAuthoringComponent]
public struct SquadShapeRotationDirection : IComponentData
{
    public float3 dir;
    public float angle;
    public quaternion averageRotation;
}
