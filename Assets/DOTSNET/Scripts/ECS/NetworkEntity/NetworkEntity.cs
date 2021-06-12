// Each networked Entity needs to have exactly one NetworkEntity component.
using Unity.Collections;
using Unity.Entities;

namespace DOTSNET
{
    // instead of GenerateAuthoring, we need custom authoring for NetworkEntity
    // because we need to assign the sceneId & assetId in OnValidate.
    public struct NetworkEntity : IComponentData
    {
        // unique Id for this entity
        // scene objects are already unique and aren't duplicated, but
        // prefabs are duplicated several times. we need a unique id.
        // => on server, this equals DOTS' unique Entity Id
        // => on client, this is the server's unique Entity Id (synced)
        // DOTS can process a LOT of entities, let's use 8 bytes
        // (which is needed for Entity unique Id anyway)
        public ulong netId;

        // unique prefab Id for this entity
        // this is used to instantiate it from PrefabManager.
        // if a client gets a spawn message, it needs to know the prefabId in
        // order to instantiate it.
        // => this is a Guid, but we store it as Bytes16 because Guid isn't
        //    blittable.
        // => if Unity has a pure DOTS Scene/Prefab Editor then we can use the
        //    unique Entity Id later.
        public Bytes16 prefabId;

        // SERVER ONLY: owner connectionId
        // monsters, npcs etc. have no owner connection
        // players, pets, etc. are owned by a connectionId, meaning that this
        // particular connectionId is allowed to call commands etc. on it
        // -> nullable so that we know if it was set or not
        //    (otherwise default would be '0', but we can't assume that all
        //     transports will start their connectionIds at '1')
        // -> use connectionId.HasValue or != null to check if it was assigned!
        public int? connectionId;

        // CLIENT ONLY: is this NetworkEntity owned by us?
        // for example, our own player, and our pet are owned by us.
        public bool owned;
    }
}