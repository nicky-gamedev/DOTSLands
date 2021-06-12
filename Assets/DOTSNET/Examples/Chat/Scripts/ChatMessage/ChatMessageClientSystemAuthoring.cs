using System;
using Unity.Entities;
using UnityEngine;

namespace DOTSNET.Examples.Chat
{
    public class ChatMessageClientSystemAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        // add system if Authoring is used
        public Type GetSystemType() => typeof(ChatMessageClientSystem);
    }

    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class ChatMessageClientSystem : NetworkClientMessageSystem<ChatMessage>
    {
        protected override void OnUpdate() {}
        protected override void OnMessage(ChatMessage message)
        {
            // convert to the actual message type
            Debug.Log("Client message: " + message.sender + ": " + message.text);

            // add message
            ChatClientSystem chatClient = (ChatClientSystem)client;
            chatClient.messages.Enqueue(message);

            // respect max entries
            if (chatClient.messages.Count > chatClient.keepMessages)
                chatClient.messages.Dequeue();
        }
    }
}