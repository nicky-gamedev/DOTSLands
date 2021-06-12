using UnityEngine;

namespace DOTSNET.Examples.Chat
{
    public class NameSelection : MonoBehaviour
    {
        // ECS world systems
        ChatClientSystem client =>
            Bootstrap.ClientWorld.GetExistingSystem<ChatClientSystem>();

        public int width = 200;
        public int height = 80;

        string input = "";

        // hello old friend
        void OnGUI()
        {
            // only while client connected & not joined yet
            if (client.state == ClientState.CONNECTED && !client.joined)
            {
                // GUI area
                float x = Screen.width / 2 - width / 2;
                float y = Screen.height / 2 - height / 2;
                GUILayout.BeginArea(new Rect(x, y, width, height));
                GUILayout.BeginVertical("Box");

                // input field with max length = 32 because the JoinWorldMessage
                // uses a NativeString32
                GUILayout.Label("Enter Nickname:");
                input = GUILayout.TextField(input, 32);

                // join button
                GUI.enabled = !string.IsNullOrWhiteSpace(input);
                if (GUILayout.Button("Join"))
                {
                    Debug.Log("Joining as: " + input + "...");
                    client.Send(new JoinMessage(input));
                }
                GUI.enabled = true;

                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                GUILayout.EndArea();
            }
        }
    }
}