// while it could be auto generated, some people need a non-internal one for
// their work flow, so let's just generate it ourselves.
// https://forum.unity.com/threads/dotsnet-high-performance-unity-ecs-networking-from-the-creator-of-mirror.880777/page-2#post-6193515
using Unity.Entities;
using UnityEngine;

namespace DOTSNET
{
	[DisallowMultipleComponent]
	public class NetworkTransformAuthoring : MonoBehaviour, IConvertGameObjectToEntity
	{
		public SyncDirection syncDirection;

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			NetworkTransform componentData = new NetworkTransform{
				syncDirection = syncDirection
			};
			dstManager.AddComponentData(entity, componentData);
		}
	}
}
