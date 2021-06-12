using Unity.Entities;

namespace DOTSNET
{
    // use SelectiveAuthoring to create/inherit it selectively
    [DisableAutoCreation]
    public class SpawnMessageSystem : NetworkClientMessageSystem<SpawnMessage>
    {
        protected override void OnUpdate() {}
        protected override void OnMessage(SpawnMessage message)
        {
            client.Spawn(message.prefabId,
                         message.netId,
                         message.owned != 0,
                         message.position,
                         message.rotation);
        }
    }
}
