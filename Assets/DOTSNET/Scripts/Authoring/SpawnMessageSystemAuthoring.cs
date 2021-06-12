// Modify NetworkTransformSystem settings via Authoring.
using System;
using UnityEngine;

namespace DOTSNET
{
    public class SpawnMessageSystemAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        // add system if Authoring is used
        public Type GetSystemType() { return typeof(SpawnMessageSystem); }
    }
}