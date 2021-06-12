using System;
using Unity.Entities;
using UnityEngine;

namespace DOTSNET.Examples.Chat
{
    public class JoinedMessageSystemAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        // add system if Authoring is used
        public Type GetSystemType() => typeof(JoinedMessageSystem);
    }

    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class JoinedMessageSystem : NetworkClientMessageSystem<JoinedMessage>
    {
        protected override void OnUpdate() {}
        protected override void OnMessage(JoinedMessage message)
        {
            ((ChatClientSystem)client).joined = true;
        }
    }
}