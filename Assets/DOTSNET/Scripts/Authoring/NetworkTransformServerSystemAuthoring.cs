// Modify NetworkTransformSystem settings via Authoring.
using System;
using UnityEngine;

namespace DOTSNET
{
    public class NetworkTransformServerSystemAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        // find NetworkServerSystem in ECS world
        NetworkTransformServerSystem system =>
            Bootstrap.ServerWorld.GetExistingSystem<NetworkTransformServerSystem>();

        // add system if Authoring is used
        public Type GetSystemType() { return typeof(NetworkTransformServerSystem); }

        // configuration
        public float interval = 0.1f;

        // apply configuration in Awake
        void Awake()
        {
            system.interval = interval;
        }
    }
}