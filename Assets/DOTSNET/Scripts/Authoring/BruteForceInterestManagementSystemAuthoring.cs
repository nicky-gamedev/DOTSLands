using System;
using UnityEngine;

namespace DOTSNET
{
    public class BruteForceInterestManagementSystemAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        // find systems in ECS world
        NetworkServerSystem server =>
            Bootstrap.ServerWorld.GetExistingSystem<NetworkServerSystem>();
        BruteForceInterestManagementSystem bruteForceSystem =>
            Bootstrap.ServerWorld.GetExistingSystem<BruteForceInterestManagementSystem>();

        public bool showSlider;
        public float visibilityRadius = 15;
        public float updateInterval = 1;

        // add system if Authoring is used
        public Type GetSystemType() => typeof(BruteForceInterestManagementSystem);

        // apply configuration in Awake once.
        void Awake() { Update(); }

        // apply configuration in Update too. it's not recommended in a real
        // project because it would overwrite settings that other ECS world
        // systems might apply. but for our demo, it's just so satisfying to
        // increase the visibility radius at runtime.
        void Update()
        {
            bruteForceSystem.visibilityRadius = visibilityRadius;
            bruteForceSystem.updateInterval = updateInterval;
        }

        // interest management visibility radius is the ultimate benchmark.
        // let's show a slider to change it at runtime. it makes the 10k demo
        // way more fun.
        void OnGUI()
        {
            if (!showSlider) return;

            // only show while server is running. not on client, etc.
            if (server.state != ServerState.ACTIVE) return;

            int height = 30;
            int width = 250;
            GUILayout.BeginArea(new Rect(Screen.width / 2 - width / 2,
                                         Screen.height - height,
                                         width,
                                         height));

            GUILayout.BeginHorizontal("Box");
            GUILayout.Label("Radius:");
            visibilityRadius = GUILayout.HorizontalSlider(visibilityRadius, 0, 200, GUILayout.Width(150));
            GUILayout.Label(visibilityRadius.ToString("F0"));
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}