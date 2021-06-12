using System;
using UnityEngine;

namespace DOTSNET.Libuv
{
    public class LibuvTransportClientAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        // find NetworkClientSystem in ECS world
        LibuvTransportClientSystem client =>
            Bootstrap.ClientWorld.GetExistingSystem<LibuvTransportClientSystem>();

        // common
        public ushort Port = 7777;

        // add to selectively created systems before Bootstrap is called
        public Type GetSystemType() => typeof(LibuvTransportClientSystem);

        // apply configuration in awake
        void Awake()
        {
            client.Port = Port;
        }
    }
}