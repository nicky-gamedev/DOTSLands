using System;
using UnityEngine;

namespace DOTSNET.Libuv
{
    public class LibuvTransportServerAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        // find NetworkServerSystem in ECS world
        LibuvTransportServerSystem server =>
            Bootstrap.ServerWorld.GetExistingSystem<LibuvTransportServerSystem>();

        // common
        public ushort Port = 7777;

        // add to selectively created systems before Bootstrap is called
        public Type GetSystemType() => typeof(LibuvTransportServerSystem);

        // apply configuration in awake
        void Awake()
        {
            server.Port = Port;
        }
    }
}