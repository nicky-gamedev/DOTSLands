// Modify NetworkTransformSystem settings via Authoring.
using System;
using UnityEngine;

namespace DOTSNET
{
    public class TransformServerMessageSystemAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        // add system if Authoring is used
        public Type GetSystemType() { return typeof(TransformServerMessageSystem); }
    }
}