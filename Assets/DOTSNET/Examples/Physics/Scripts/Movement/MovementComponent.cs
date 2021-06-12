using Unity.Entities;

namespace DOTSNET.Examples.Physics
{
    [GenerateAuthoringComponent]
    public struct MovementComponent : IComponentData
    {
        public float force;
    }
}
