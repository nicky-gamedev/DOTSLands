using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace DOTSNET
{
    public class PrefabSystemAuthoring : MonoBehaviour, IDeclareReferencedPrefabs
    {
        // registered spawnable prefabs
        // of type NetworkEntity so only those can be added. avoids an extra check.
        public List<NetworkEntityAuthoring> prefabs = new List<NetworkEntityAuthoring>();

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            // TODO
            // DeclareReferencePrefabs only works for DefaultWorld.
            // Try this again instead of manual conversion in PrefabSystem.
            // Maybe it will work some day.
            //referencedPrefabs.AddRange(prefabs.Select(comp => comp.gameObject));

            // instead we declare it in our PrefabSystem manually...

            // register in all worlds that have a PrefabSystem
            // (limiting it to Bootstrap.Server/ClientWorld is not future safe)
            foreach (World world in World.All)
            {
                // does this world have a PrefabSystem?
                PrefabSystem prefabSystem = world.GetExistingSystem<PrefabSystem>();
                if (prefabSystem != null)
                {
                    foreach (NetworkEntityAuthoring prefab in prefabs)
                    {
                        // register in world based on authoring assetId, because the
                        // Entity doesn't have a NetworkEntity component yet.
                        if (prefabSystem.RegisterGameObjectPrefab(prefab.prefabId, prefab.gameObject))
                        {
                            Debug.Log(prefab.name + " prefab registered with prefabId=" + prefab.prefabId + " in world: " + world.Name);
                        }
                        else Debug.LogError("Registering " + prefab.name + " with prefabId=" + prefab.prefabId + " failed. Was it already registered?");
                    }
                }
            }
        }
    }
}