using UnityEngine;
using Unity.Entities;
using Unity.Physics;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Reese.Nav;
using DOTSNET;
using System.Linq;

[ClientWorld]
[UpdateInGroup(typeof(ClientConnectedSimulationSystemGroup))]
public class TroopPathfindingSystem : SystemBase
{
    BeginSimulationEntityCommandBufferSystem bufferSystem;
    Entity shapeParent;

    NativeList<Entity> selectedSoldiers;
    NativeList<Entity> playerSoldiers;

    NativeList<Entity> childsOrdered;

    Camera cam;

    protected override void OnCreate()
    {
        base.OnCreate();
        selectedSoldiers = new NativeList<Entity>(1000, Allocator.Persistent);
        playerSoldiers = new NativeList<Entity>(1000, Allocator.Persistent);
        childsOrdered = new NativeList<Entity>(1000, Allocator.Persistent);

    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        selectedSoldiers.Dispose();
        playerSoldiers.Dispose();
        childsOrdered.Dispose();
    }

    protected override void OnStartRunning()
    {
        bufferSystem = Bootstrap.ClientWorld.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
        cam = Camera.main;
        base.OnStartRunning();
    }

    protected override void OnUpdate()
    {
        #region Ordering Childs
        if (shapeParent == Entity.Null) shapeParent = GetEntityQuery(typeof(SquadShapeParentTag)).GetSingletonEntity();
        if (childsOrdered.Length < 1)
        {
            var indexesToSort = new NativeList<int>(1000, Allocator.Temp);
            var childs = GetBuffer<Child>(shapeParent);

            for (int i = 0; i < childs.Length; i++)
            {
                indexesToSort.Add(childs[i].Value.Index);
            }

            var index = SortChilds(indexesToSort.ToArray(), indexesToSort.Length);
            for (int i = 0; i < index.Length; i++)
            {
                for (int j = 0; j < childs.Length; j++)
                {
                    if (childs[j].Value.Index == index[i])
                    {
                        childsOrdered.Add(childs[j].Value);
                        Debug.Log(childs[j].Value + ", with index " + index[i] + "was added in position " + (childsOrdered.Length - 1));
                        continue;
                    }
                }
            }
            indexesToSort.Dispose();
        }
        #endregion

        #region References
        EntityCommandBuffer ecb = bufferSystem.CreateCommandBuffer();
        #endregion

        #region Player List
        NativeList<Entity> _playerSoldiers = playerSoldiers;

        Entities.WithoutBurst().WithAll<PlayerNavTag>().ForEach(
            (Entity e) =>
            {
                NetworkEntity net;
                try
                {
                    net = GetComponent<NetworkEntity>(GetComponent<PlayerAgentComponent>(e).player);
                }
                catch
                {
                    return;
                }

                if (!net.owned) return;
                if (_playerSoldiers.Contains(e)) return;
                _playerSoldiers.Add(e);
            }).Run();
        #endregion

        #region Player Selection 
        NativeList<Entity> _selectedSoldiers = selectedSoldiers;
        var _cam = cam;

        Entities.WithoutBurst().ForEach(
            (Entity entity, in SelectionBox selectionBox) =>
            {
                if (selectionBox.released) return;
                _selectedSoldiers.Clear();
                for (int i = 0; i < _playerSoldiers.Length; i++)
                {
                    var ltw = GetComponent<LocalToWorld>(_playerSoldiers[i]);
                    float3 screenPos = _cam.WorldToScreenPoint(ltw.Position);

                    if (
                    screenPos.x > selectionBox.min.x &&
                    screenPos.x < selectionBox.max.x &&
                    screenPos.y > selectionBox.min.y &&
                    screenPos.y < selectionBox.max.y)
                    {
                        if (_selectedSoldiers.Contains(_playerSoldiers[i])) return;
                        _selectedSoldiers.Add(_playerSoldiers[i]);
                    }
                }
            }).Run();
        #endregion

        #region Adding component to query selected soldiers
        for (int i = 0; i < playerSoldiers.Length; i++)
        {
            var item = playerSoldiers[i];
            Entity entity = Entity.Null;

            try
            {
                entity = GetComponent<PlayerAgentComponent>(item).player;
            }
            catch
            {
                playerSoldiers.RemoveAt(i);
                selectedSoldiers.Clear();
                continue;
            }

            if (selectedSoldiers.Contains(item))
            {
                try
                {
                    EntityManager.AddComponentData(entity, new SelectedTroopComponent { });
                }
                catch
                {
                    playerSoldiers.RemoveAt(i);
                    selectedSoldiers.Clear();
                    continue;
                }
            }
            else
            {
                if (HasComponent<SelectedTroopComponent>(entity))
                {
                    EntityManager.RemoveComponent(entity, typeof(SelectedTroopComponent));
                }
            }
        }
        #endregion

        if (!GetComponent<SquadShapeParentTag>(shapeParent).posUpdated) return;
        if (selectedSoldiers.Length <= 0) return;

        #region Raycast References
        CollisionFilter filter = new CollisionFilter
        {
            BelongsTo = 2,
            CollidesWith = ~0u,
            GroupIndex = 0
        };
        NativeList<float3> positions = new NativeList<float3>(Allocator.TempJob);
        #endregion

        #region Getting troop shape children
        for (int i = 0; i < childsOrdered.Length; i++)
        {
            Entity thisChild = childsOrdered[i];

            var pos = GetComponent<LocalToWorld>(thisChild).Position;
            var downHit = MiscFunctions.CastRaycast(pos, new float3(pos.x, pos.y * -100, pos.z), filter);
            var upperHit = MiscFunctions.CastRaycast(pos, new float3(pos.x, pos.y * 100, pos.z), filter);
            if (!math.all(downHit.Position == float3.zero))
            {
                positions.Add(downHit.Position);
                continue;
            }
            else if (!math.all(upperHit.Position == float3.zero))
            {
                positions.Add(upperHit.Position);
                continue;
            }
            else
            {
                positions.Add(pos);
            }
        }
        #endregion

        #region Adding Destination to Selected Soldiers
        Job.WithoutBurst().WithCode(
            () =>
            {
                for (int i = 0; i < _selectedSoldiers.Length; i++)
                {
                    ecb.AddComponent(_selectedSoldiers[i], new NavNeedsDestination
                    {
                        Destination = positions[i],
                        Teleport = false
                    });
                }
            }).Run();

        this.CompleteDependency();
        SetComponent(shapeParent, new SquadShapeParentTag { posUpdated = false });
        positions.Dispose();
        #endregion
    }

    public int[] SortChilds(int[] data, int size)
    {
        int i, j;
        for (i = 1; i < size; i++)
        {
            int item = data[i];
            int ins = 0;
            for (j = i - 1; j >= 0 && ins != 1;)
            {
                if (item < data[j])
                {
                    data[j + 1] = data[j];
                    j--;
                    data[j + 1] = item;
                }
                else ins = 1;
            }
        }
        return data;
    }
}

