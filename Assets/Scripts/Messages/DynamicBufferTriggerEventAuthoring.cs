using Unity.Entities;
using Unity.Physics.Stateful;
using UnityEngine;

public class DynamicBufferTriggerEventAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddBuffer<StatefulTriggerEvent>(entity);
    }
}
