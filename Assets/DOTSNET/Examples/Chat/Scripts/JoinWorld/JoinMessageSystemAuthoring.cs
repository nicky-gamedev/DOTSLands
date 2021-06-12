using System;
using Unity.Entities;
using UnityEngine;

namespace DOTSNET.Examples.Chat
{
    public class JoinMessageSystemAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        // add system if Authoring is used
        public Type GetSystemType() => typeof(JoinMessageSystem);
    }

    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class JoinMessageSystem : NetworkServerMessageSystem<JoinMessage>
    {
        protected override void OnUpdate() {}
        protected override bool RequiresAuthentication() { return true; }
        protected override void OnMessage(int connectionId, JoinMessage message)
        {
            Debug.Log("Server: client joining as " + message.name);

            // set connection nickname, reply with Joined message
            ChatServerSystem chatServer = (ChatServerSystem)server;
            if (!chatServer.names.ContainsKey(connectionId))
            {
                chatServer.names[connectionId] = message.name;
                server.Send(connectionId, new JoinedMessage());
            }
            // don't allow joining twice
            else
            {
                Debug.LogWarning("ConnectionId " + connectionId + " already joined!");
                server.Disconnect(connectionId);
            }
        }
    }
}