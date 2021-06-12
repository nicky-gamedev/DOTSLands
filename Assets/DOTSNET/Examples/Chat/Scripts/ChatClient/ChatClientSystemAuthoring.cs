// inherit from NetworkClientSystem and add some chat state
using System;
using System.Collections.Generic;
using Unity.Entities;

namespace DOTSNET.Examples.Chat
{
    public class ChatClientSystemAuthoring : NetworkClientAuthoring
    {
        // the system
        ChatClientSystem client =>
            Bootstrap.ClientWorld.GetExistingSystem<ChatClientSystem>();

        // add system if Authoring is used
        public override Type GetSystemType() => typeof(ChatClientSystem);

        // configuration
        public int keepMessages = 100;

        // apply configuration
        protected override void Awake()
        {
            base.Awake();
            client.keepMessages = keepMessages;
        }
    }

    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class ChatClientSystem : NetworkClientSystem
    {
        // state
        public bool joined;

        // messages
        public int keepMessages = 100;
        public Queue<ChatMessage> messages = new Queue<ChatMessage>();

        // reset state when disconnected
        protected override void OnDisconnected()
        {
            joined = false;
            messages.Clear();
        }
    }
}