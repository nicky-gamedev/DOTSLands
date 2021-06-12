// a simple start/stop GUI
using UnityEngine;

namespace DOTSNET
{
    public class NetworkServerHUD : MonoBehaviour
    {
        void OnGUI()
        {
            // get component
            NetworkServerAuthoring server = GetComponent<NetworkServerAuthoring>();

            // create GUI area
            GUILayout.BeginArea(new Rect(15, 15, 220, 150));

            // start server
            if (server.state == ServerState.INACTIVE)
            {
                if (GUILayout.Button("Start Server"))
                {
                    server.StartServer();
                }
            }
            // stop server
            else
            {
                if (GUILayout.Button("Stop Server"))
                {
                    server.StopServer();
                }
            }

            // end of GUI area
            GUILayout.EndArea();
        }
    }
}