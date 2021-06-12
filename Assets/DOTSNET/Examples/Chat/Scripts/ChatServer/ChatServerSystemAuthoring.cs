using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace DOTSNET.Examples.Chat
{
    public class ChatServerSystemAuthoring : NetworkServerAuthoring
    {
        // add system if Authoring is used
        public override Type GetSystemType() => typeof(ChatServerSystem);
    }

    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class ChatServerSystem : NetworkServerSystem
    {
        // nicknames per connection
        public Dictionary<int, FixedString32> names = new Dictionary<int, FixedString32>();

        protected override void OnDisconnected(int connectionId)
        {
            // remove from names if the connectionId disconnected
            names.Remove(connectionId);
        }
    }
}