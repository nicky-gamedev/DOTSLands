using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using DOTSNET;
using Unity.Collections;

[UpdateInGroup(typeof(ClientConnectedSimulationSystemGroup))]
public class SpawnMeshListener : NetworkClientMessageSystem<SpawnMeshMessage>
{
    //Search loop attributes
    ulong idToSearch;
    int limitToSearch = 500;
    int searches;

    //Delay query update attributes
    float timeToWait = 1f;
    float timer;

    GameObject mesh;

    protected override void OnCreate()
    {
        base.OnCreate();
        mesh = Resources.Load("Prefabs/PlayerGFX") as GameObject;
    }

    protected override void OnMessage(SpawnMeshMessage message)
    {
        if (client.spawned.TryGetValue(message.id, out Entity entity))
        {
            SpawnMesh(entity);
        }
        else
        {
            idToSearch = message.id;
            searches = 0;
        }
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        timer = timeToWait;
    }

    protected override void OnUpdate()
    {
        if (client.spawned.TryGetValue(idToSearch, out Entity entity) && searches < limitToSearch)
        {
            SpawnMesh(entity);
            searches = limitToSearch;
        }
        else
        {
            searches++;
        }

        if (timer > 0)
        {
            timer -= Time.DeltaTime;
        }
        else if (timer <= 0)
        {
            UpdateQuery();
            timer = timeToWait;
        }

    }

    void SpawnMesh(Entity entity)
    {
        GameObject temp = MonoBehaviour.Instantiate(mesh);

        temp.GetComponent<AnimationManagement>().entityToFollow = entity;

        var input = EntityManager.GetComponentData<AnimationInputComponent>(entity);
        input.haveMesh = true;
        EntityManager.SetComponentData(entity, input);
    }

    void UpdateQuery()
    {
        NativeArray<Entity> playerQuery = GetEntityQuery(typeof(PlayerTag)).ToEntityArray(Allocator.TempJob);
        NativeArray<Entity> meshQuery = World.DefaultGameObjectInjectionWorld.
            EntityManager.CreateEntityQuery(typeof(MeshTag)).ToEntityArray(Allocator.TempJob);

        if(playerQuery.Length == meshQuery.Length)
        {
            playerQuery.Dispose();
            meshQuery.Dispose();
            return;
        }

        if (playerQuery.Length > meshQuery.Length)
        {
            for (int i = 0; i < playerQuery.Length; i++)
            {
                if (EntityManager.GetComponentData<AnimationInputComponent>(playerQuery[i]).haveMesh) continue;

                SpawnMesh(playerQuery[i]);
            }
        }
        else if(playerQuery.Length < meshQuery.Length)
        {
            for (int i = 0; i < meshQuery.Length; i++)
            {
                World.DefaultGameObjectInjectionWorld.EntityManager.DestroyEntity(meshQuery[i]);
            }
            foreach(Entity e in playerQuery)
            {
                SpawnMesh(e);
            }
        }

        playerQuery.Dispose();
        meshQuery.Dispose();
    }
}
