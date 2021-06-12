using Unity.Mathematics;

namespace DOTSNET.Examples.Benchmark
{
    public static class Utils
    {
        // MoveTowards moves towards target, but not beyond it.
        public static float3 movetowards(float3 current, float3 target, float step)
        {
            // calculate direction
            float3 direction = target - current;

            // calculate distance
            float distance = math.distance(target, current);

            // return target if we would move beyond it
            if (distance <= step)
                return target;

            // move closer
            return current + math.normalizesafe(direction) * step;
        }
    }
}