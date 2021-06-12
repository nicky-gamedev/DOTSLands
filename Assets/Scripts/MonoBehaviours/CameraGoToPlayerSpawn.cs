using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using DOTSNET;
using Unity.Collections;
using Unity.Mathematics;
public class CameraGoToPlayerSpawn : MonoBehaviour
{
    bool repos;
    [SerializeField] float3 offset;

    void Update()
    {
        if (!repos)
        {
            var em = Bootstrap.ClientWorld.EntityManager;
            var query = em.CreateEntityQuery(typeof(PlayerDataComponent), typeof(NetworkEntity)).ToEntityArray(Allocator.TempJob);
            foreach (var item in query)
            {
                if (em.GetComponentData<NetworkEntity>(item).owned)
                {
                    transform.position = em.GetComponentData<Translation>(item).Value + offset;
                    repos = true;
                }
            }
            query.Dispose();
        }
    }
}
