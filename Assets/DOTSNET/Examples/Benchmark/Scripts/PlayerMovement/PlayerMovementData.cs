using Unity.Entities;

namespace DOTSNET.Examples.Benchmark
{
    [GenerateAuthoringComponent]
    public struct PlayerMovementData : IComponentData
    {
        // movement speed in m/s
        public float speed;
    }
}
