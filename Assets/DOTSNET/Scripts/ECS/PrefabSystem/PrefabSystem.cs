// The PrefabSystem
//
// Basic facts:
// * Server & Client worlds are always separated in memory
// * We can have Prefab Entities that are hidden and not updated
// * We can make anything a Prefab Entity
//
// So instead of treating scene objects differently, we simply convert all of
// them to prefabs and then remove all of them from the scene.
// + There is no more distinction between them. All can be spawned by Guid.
// + There are no hidden scene objects that need special treatment.
// + The server can STILL have them in the world by default by simply spawning
//   all scene objects in OnStartRunning once.
// + There is no more assetId & sceneId. Only prefabId.
//
// It's perfect.
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace DOTSNET
{
    // AlwaysUpdate is needed for OnStartRunning ForEach to be called.
    // Because OnUpdate is empty, OnStartRunning would never be called otherwise
    // (which might be an ECS bug)
    [AlwaysUpdateSystem]
    // both Client & Server worlds need prefabs
    [ClientWorld, ServerWorld]
    public class PrefabSystem : SystemBase
    {
        // authoring ///////////////////////////////////////////////////////////
        // (this can be removed once we have a pure ECS Scene/Prefab Editor)
        //
        // all GameObject prefabs by prefabId Guid
        // (this is only used once for conversion, so 'Guid' key is fine here)
        Dictionary<Guid, GameObject> gameObjectPrefabs = new Dictionary<Guid, GameObject>();

        // keep reference to the conversion settings so we can clean them up
        GameObjectConversionSettings conversionSettings;
        BlobAssetStore blobAssetStore;

        // Entity prefabs //////////////////////////////////////////////////////
        // all known spawnable prefabs by <prefabId, Entity>
        // note: prefabId is a Guid, but we store it as Bytes16 because it's
        //       Bytes16 in all components and in all messages. otherwise we
        //       would have to convert it back to Guid, which allocates 16 bytes
        // note: has to be public so that other systems can filter out all the
        //       player prefabs etc.
        public readonly Dictionary<Bytes16, Entity> prefabs = new Dictionary<Bytes16, Entity>();

        // all known spawnable scene objects by <prefabId, Entity>
        // we store them in a separate dictionary so that the server can spawn
        // all of them in OnStartRunning once.
        // (scene objects are expected to be in the scene by default!)
        public readonly Dictionary<Bytes16, Entity> scenePrefabs = new Dictionary<Bytes16, Entity>();

        protected override void OnCreate()
        {
            // create conversion settings once
            blobAssetStore = new BlobAssetStore();
            conversionSettings = new GameObjectConversionSettings(
                World,
                GameObjectConversionUtility.ConversionFlags.AssignName,
                blobAssetStore
            );
        }

        internal void ConvertGameObjectPrefabs()
        {
            // convert all the GameObject prefabs
            foreach (KeyValuePair<Guid, GameObject> kvp in gameObjectPrefabs)
            {
                Debug.Log("Converting Prefab: " + kvp.Value + " with prefabId=" + kvp.Key + " to Entity Prefab in World: " + World);
                Entity entityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(kvp.Value, conversionSettings);
                Bytes16 prefabId = Conversion.GuidToBytes16(kvp.Key);
                prefabs[prefabId] = entityPrefab;
            }

            // clear list so we don't convert those again if the function is
            // called again
            gameObjectPrefabs.Clear();
        }

        internal void ConvertSceneObjectPrefabs()
        {
            Entities.ForEach((ref NetworkEntity networkEntity, in Entity entity) =>
            {
                // log message for debugging (logging 10k each time is too much)
                //Debug.Log("Converting Scene Object: " + EntityManager.GetName(entity) + " to Entity Prefab in World: " + World);

                // add a Prefab component so it's hidden & not updated
                EntityManager.AddComponentData(entity, new Prefab());

                // get prefabId
                Bytes16 prefabId = networkEntity.prefabId;

                // register if it hasn't been registered yet
                if (prefabs.ContainsKey(prefabId))
                {
                    // this can happen if a registered prefab is also dragged
                    // into the scene.
                    // in that case, it's not obvious which one we should use
                    // and the one in the scene might have modifications.
                    // it's simply not supported at the moment.
#if UNITY_EDITOR
                    Debug.LogError("PrefabSystem: World: " + World + " Scene Object: " + EntityManager.GetName(entity) + " prefabId: " + Conversion.Bytes16ToGuid(prefabId) + " already used by registered Prefab: " + EntityManager.GetName(prefabs[prefabId]) + ". Please either remove it from the PrefabSystem's registered Prefabs list and keep it in the scene, or remove it from the scene and keep it in the Prefabs list!");
#else
                    Debug.LogError("PrefabSystem: World: " + World + " Scene Object: " +                       entity  + " prefabId: " + Conversion.Bytes16ToGuid(prefabId) + " already used by registered Prefab: " +                       prefabs[prefabId]  + ". Please either remove it from the PrefabSystem's registered Prefabs list and keep it in the scene, or remove it from the scene and keep it in the Prefabs list!");
#endif
                }
                else if (scenePrefabs.ContainsKey(prefabId))
                {
#if UNITY_EDITOR
                    Debug.LogError("PrefabSystem: " + World + " Scene Object: " + EntityManager.GetName(entity) + " prefabId: " + Conversion.Bytes16ToGuid(prefabId) + " already used by Scene Prefab: " + EntityManager.GetName(scenePrefabs[prefabId]));
#else
                    Debug.LogError("PrefabSystem: " + World + " Scene Object: " +                       entity  + " prefabId: " + Conversion.Bytes16ToGuid(prefabId) + " already used by Scene Prefab: " +                       scenePrefabs[prefabId] );
#endif
                }
                else
                {
                    scenePrefabs[prefabId] = entity;
                }
            })
            .WithStructuralChanges()
            .Run();
        }

        protected override void OnStartRunning()
        {
            // convert all GameObject prefabs to Entity prefabs when starting.
            // * OnCreate is too early because Register hasn't been called yet
            // * Register is too early because we can't do manual conversions
            //   during Unity's ConvertToEntity. we would get 'Collection was
            //   modified' InvalidOperationException errors.
            ConvertGameObjectPrefabs();

            // convert all scene objects to Prefabs when starting.
            // IMPORTANT: this should be called before server/client are started
            //            and it is because PrefabSystem is not in the
            //            simulation group. so it gets run immediately.
            ConvertSceneObjectPrefabs();

            // PrefabSystem should only ever run once.
            // there is no need to update anything.
            // if we stay Enabled, OnStartRunning might be called again, at
            // which point we would convert all scene objects for no good reason
            // again.
            // let's just disable self.
            Enabled = false;
        }

        protected override void OnUpdate() {}

        protected override void OnDestroy()
        {
            // clean up all native collections
            blobAssetStore.Dispose();
        }

        // get a prefab by prefabId
        public bool Get(Bytes16 prefabId, out Entity prefab)
        {
            return prefabs.TryGetValue(prefabId, out prefab) ||
                   scenePrefabs.TryGetValue(prefabId, out prefab);
        }

        // authoring ///////////////////////////////////////////////////////////
        // register a GameObject prefab by prefabId
        // note: we need to store the GameObject prefabs and convert them at
        //       runtime because the IDeclareReferencePrefabs way only works in
        //       the default world. So we need to do it manually for our server/
        //       client worlds.
        // we use Guid for ease of use and convert it to Bytes16 internally
        public bool RegisterGameObjectPrefab(Guid prefabId, GameObject prefab)
        {
            if (!gameObjectPrefabs.ContainsKey(prefabId))
            {
                Debug.Log("PrefabSystem.Register: " + prefabId + " prefab=" + prefab + " world=" + World);
                gameObjectPrefabs[prefabId] = prefab;
                return true;
            }
            return false;
        }
    }
}
