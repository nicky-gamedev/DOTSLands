// Moving 10k monsters is hard work. We use Jobs for maximum performance:
//
// Benchmark:
//   Server & Client in Editor
//   Server: 10k monsters
//   Client: 15 visibility radius (=around 7k monsters)
//
//   ___________________|_EntityDebugger(ms)_|____FPS____
//   ComponentSystem    |    12ms -   17ms   |  44 -  81
//   JobComponentSystem |  0.05ms - 0.07ms   | 150 - 196
//   Burst              |  0.02ms - 0.04ms   | 170 - 199
//
//   avg 15ms / avg 0.06ms => 250x faster!
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DOTSNET.Examples.Benchmark
{
    [ServerWorld]
    [UpdateInGroup(typeof(ServerActiveSimulationSystemGroup))]
    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class MonsterMovementSystem : SystemBase
    {
        protected override void OnStartRunning()
        {
            // set up the start positions once
            Entities.ForEach((ref MonsterMovementData movement,
                              in Translation translation) =>
            {
                movement.startPosition = translation.Value;
            })
            .Run();
        }

        protected override void OnUpdate()
        {
            // new random for each update
            // (time+1 because seed must be non-zero to avoid exceptions)
            uint seed = 1 + (uint)Time.ElapsedTime;
            Random random = new Random(seed);

            // foreach
            float deltaTime = Time.DeltaTime;
            Entities.ForEach((ref Translation translation,
                              ref MonsterMovementData movement) =>
            {
                // are we moving?
                if (movement.isMoving)
                {
                    // check if destination was reached yet
                    if (math.distance(translation.Value, movement.destination) <= 0.01f)
                    {
                        movement.isMoving = false;
                    }
                    // otherwise move towards destination
                    else
                    {
                        translation.Value = Utils.movetowards(translation.Value, movement.destination, movement.speed * deltaTime);
                    }
                }
                // we are not moving
                else
                {
                    // move this time?
                    float r = random.NextFloat(); // [0,1)
                    if (r <= movement.moveProbability * deltaTime)
                    {
                        // calculate random destination in moveDistance
                        float2 circle2D = random.NextFloat2Direction();
                        float3 direction = new float3(circle2D.x, 0, circle2D.y);
                        float3 destination = translation.Value + direction * movement.moveDistance;

                        // only go there if it's within a circle around start
                        // so we don't wander off into nirvana
                        if (math.distance(movement.startPosition, destination) <= movement.moveDistance)
                        {
                            movement.destination = destination;
                            movement.isMoving = true;
                        }
                    }
                }
            })
            .Schedule();
        }
    }
}
