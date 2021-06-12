using Unity.Entities;

namespace DOTSNET
{
    // use SelectiveAuthoring to create/inherit it selectively
    [DisableAutoCreation]
    public class UnspawnMessageSystem : NetworkClientMessageSystem<UnspawnMessage>
    {
        protected override void OnUpdate() {}
        protected override void OnMessage(UnspawnMessage message)
        {
            client.Unspawn(message.netId);
        }
    }
}
