// Applies the TransformMessage to the Entity.
// There is no interpolation yet, only the bare minimum.
using Unity.Entities;
using Unity.Transforms;

namespace DOTSNET
{
    // use SelectiveAuthoring to create/inherit it selectively
    [DisableAutoCreation]
    public class TransformServerMessageSystem : NetworkServerMessageSystem<TransformMessage>
    {
        protected override void OnUpdate() {}
        protected override bool RequiresAuthentication() { return true; }
        protected override void OnMessage(int connectionId, TransformMessage message)
        {
            // find entity by netId
            if (server.spawned.TryGetValue(message.netId, out Entity entity))
            {
                // apply position & rotation
                SetComponent(entity, new Translation{Value = message.position});
                SetComponent(entity, new Rotation{Value = message.rotation});
            }
        }
    }
}
