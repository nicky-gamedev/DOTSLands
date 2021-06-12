using System;
using UnityEngine;

namespace DOTSNET.Examples.Physics
{
    public class JoinWorldMessageSystemAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        // add system if Authoring is used
        public Type GetSystemType() => typeof(JoinWorldMessageSystem);
    }
}