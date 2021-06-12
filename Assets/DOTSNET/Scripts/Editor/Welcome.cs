// Shows either a welcome message, or warning about a recommended Unity version.
using UnityEditor;
using UnityEngine;

namespace DOTSNET.Editor
{
    static class Welcome
    {
        [InitializeOnLoadMethod]
        static void OnInitializeOnLoad()
        {
            // InitializeOnLoad is called on start and after each rebuild,
            // but we only want to show this once per editor session.
            if (!SessionState.GetBool("DOTSNET_WELCOME", false))
            {
                SessionState.SetBool("DOTSNET_WELCOME", true);

#if UNITY_2020_1
                Debug.Log("DOTSNET | u3d.as/YUi | https://discord.gg/2gNKN78");
#else
                Debug.LogWarning("DOTSNET works best with Unity 2020.1: https://unity3d.com/get-unity/download/archive");
#endif
            }
        }
    }
}