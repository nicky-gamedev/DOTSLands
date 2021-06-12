// Modify NetworkTransformSystem settings via Authoring.
using System;
using UnityEngine;

namespace DOTSNET
{
    public class NetworkTransformClientSystemAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        // find NetworkServerSystem in ECS world
        NetworkTransformClientSystem system =>
            Bootstrap.ClientWorld.GetExistingSystem<NetworkTransformClientSystem>();

        // add system if Authoring is used
        public Type GetSystemType() { return typeof(NetworkTransformClientSystem); }

        // configuration
        public float interval = 0.1f;

        // apply configuration in Awake
        void Awake()
        {
            system.interval = interval;
        }
    }
}