using Unity.Entities;

namespace DOTSNET.Examples.Pong
{
    [GenerateAuthoringComponent]
    public struct RacketMovementData : IComponentData
    {
        // movement speed in m/s
        public float speed;

        // bounds
        public float maxZ;
        public float minZ;
    }
}
