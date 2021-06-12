// Enables the MemoryTransport selectively
using System;
using UnityEngine;

namespace DOTSNET.MemoryTransport
{
    public class MemoryTransportServerAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        // add system if Authoring is used
        public Type GetSystemType() => typeof(MemoryTransportServerSystem);
    }
}