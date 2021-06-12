// Unity's built in ConvertToEntity component adds it to the default world.
// We need a component that adds to Server/ClientWorlds.
using Unity.Entities;
using UnityEngine;

namespace DOTSNET
{
    public enum TargetWorld { Both, ServerWorld, ClientWorld }

    public class ConvertToNetworkEntity : ConvertToEntity
    {
        public TargetWorld targetWorld = TargetWorld.Both;

        void Awake()
        {
            // get default world
            World defaultWorld = World.DefaultGameObjectInjectionWorld;

            // find the ConvertToEntitySystem
            ConvertToEntitySystem convertSystem = defaultWorld.GetOrCreateSystem<ConvertToEntitySystem>();

            // find both worlds
            if (Bootstrap.ClientWorld != null && Bootstrap.ServerWorld != null)
            {
                // add to ConvertToEntitySystem
                if (targetWorld == TargetWorld.Both)
                {
                    convertSystem.AddToBeConverted(Bootstrap.ServerWorld, this);
                    convertSystem.AddToBeConverted(Bootstrap.ClientWorld, this);
                }
                else if (targetWorld == TargetWorld.ServerWorld)
                {
                    convertSystem.AddToBeConverted(Bootstrap.ServerWorld, this);
                }
                else if (targetWorld == TargetWorld.ClientWorld)
                {
                    convertSystem.AddToBeConverted(Bootstrap.ClientWorld, this);
                }
            }
            else Debug.LogError("ConvertToNetworkEntity: failed to find server and client worlds!");
        }
    }
}
