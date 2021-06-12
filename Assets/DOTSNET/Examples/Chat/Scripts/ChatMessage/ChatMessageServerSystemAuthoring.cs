using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace DOTSNET.Examples.Chat
{
    public class ChatMessageServerSystemAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        // add system if Authoring is used
        public Type GetSystemType() => typeof(ChatMessageServerSystem);
    }

    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class ChatMessageServerSystem : NetworkServerMessageSystem<ChatMessage>
    {
        protected override void OnUpdate() {}
        protected override bool RequiresAuthentication() { return true; }
        protected override void OnMessage(int connectionId, ChatMessage message)
        {
            // get name from manager
            ChatServerSystem chatServer = (ChatServerSystem)server;
            if (chatServer.names.TryGetValue(connectionId, out FixedString32 name))
            {
                Debug.Log("Server message: " + name + ": " + message.text);

                // put name into message
                message.sender = name;

                // broadcast to all clients that joined with a name
                foreach (int clientConnectionId in chatServer.names.Keys)
                {
                    server.Send(clientConnectionId, message);
                }
            }
            else
            {
                Debug.LogWarning("Server failed to find name for " + connectionId);
                server.Disconnect(connectionId);
            }
        }
    }
}