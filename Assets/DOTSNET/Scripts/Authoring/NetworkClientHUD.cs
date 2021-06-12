// a simple connect/disconnect GUI
using UnityEngine;

namespace DOTSNET
{
    public class NetworkClientHUD : MonoBehaviour
    {
        string address = "localhost";

        void OnGUI()
        {
            // get component
            NetworkClientAuthoring client = GetComponent<NetworkClientAuthoring>();

            // create GUI area
            GUILayout.BeginArea(new Rect(15, 55, 220, 150));

            // connect client
            if (client.state == ClientState.DISCONNECTED)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Connect Client"))
                {
                    client.Connect(address);
                }
                address = GUILayout.TextField(address);
                GUILayout.EndHorizontal();
            }
            // disconnect client
            else if (client.state != ClientState.DISCONNECTED)
            {
                if (GUILayout.Button("Disconnect Client"))
                {
                    client.Disconnect();
                }
            }

            // end of GUI area
            GUILayout.EndArea();
        }
    }
}