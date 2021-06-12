using System;
using UnityEngine;

namespace DOTSNET.Examples.Benchmark
{
    public class JoinWorldMessageSystemAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        // add system if Authoring is used
        public Type GetSystemType() => typeof(JoinWorldMessageSystem);
    }
}