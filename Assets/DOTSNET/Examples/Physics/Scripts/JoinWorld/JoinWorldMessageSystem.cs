// Client sends JoinWorldMessage to the server to indicate that it's finished
// selecting characters / being in lobby, and is ready to join the world now.
using Unity.Entities;

namespace DOTSNET.Examples.Physics
{
    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class JoinWorldMessageSystem : NetworkServerMessageSystem<JoinWorldMessage>
    {
        // dependencies
        [AutoAssign] protected PrefabSystem prefabSystem;

        protected override void OnUpdate() {}
        protected override bool RequiresAuthentication() { return true; }
        protected override void OnMessage(int connectionId, JoinWorldMessage message)
        {
            // find prefab with that prefabId
            // note: this is just a simple example. in a real project we should
            //       check if the assetId is actually a player prefab, not a
            //       monster prefab etc.
            if (prefabSystem.Get(message.playerPrefabId, out Entity prefab))
            {
                // instantiate player prefab
                Entity player = EntityManager.Instantiate(prefab);

                // join the world with that player prefab
                server.JoinWorld(connectionId, player);
            }
        }
    }
}