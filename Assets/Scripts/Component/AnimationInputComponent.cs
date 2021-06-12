using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;

[GenerateAuthoringComponent]
public struct AnimationInputComponent : IComponentData
{
    public bool haveMesh;

    public bool moving;
    public float3 forward;
}
