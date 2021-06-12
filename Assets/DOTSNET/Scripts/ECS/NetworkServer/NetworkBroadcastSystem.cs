// Base class for all broadcast systems.
//
// For every component type that should be synced over the network, we need one
// BroadcastSystem.
//
// The alternative is to have one BroadcastSystem that automatically serializes
// all components that implement something like a NetworkComponent interface.
// This would be great for minimize bandwidth (one big packet with the full
// state, instead of many smaller ones), but DOTS has no way to get all
// components that implement NetworkComponent.
// (we can get all component types, and we can filter out the ones that
//  implement the NetworkComponent interface, but we can't call EntityManager
//  .GetComponentData(type). It only works via GetComponentData<T>.
//
// The BroadcastSystem approach is the cleaner, pure ECS way to do it.
// => Right now, it sends one NetworkMessage per component (costs bandwidth)
// => Later on if we have a MessageQueue then we can combine all messages every
//    time we send them out.
// => In Unity, bandwidth was never the issue for high scale multiplayer games.
//    The issue was always CPU power, so this is a good trade-off to make.
using Unity.Entities;

namespace DOTSNET
{
    // note: [AlwaysUpdateSystem] isn't needed because we should only broadcast
    //       if there are entities around.
    [ServerWorld]
    [UpdateInGroup(typeof(ServerActiveSimulationSystemGroup))]
    // ComponentSystem for now because Jobs can't send packets
    // We can try a Job later if we queue packets.
    public abstract class NetworkBroadcastSystem : SystemBase
    {
        // dependencies
        [AutoAssign] protected NetworkServerSystem server;

        // send interval can be modified by Authoring
        public float interval = 0.1f;
        double lastTime;

        // broadcast the component's state for all entities
        // note: we CAN NOT make this an abstract
        //       Broadcast<T> where T : ComponentData
        //       for three reasons:
        //       1. Entities.ForEach can't have both <NetworkEntity, T>
        //          because <T> could be of type NetworkEntity, which the
        //          compiler doesn't allow. But we need NetworkEntity to know
        //          if it's an owner.
        //          (we could call EntityManager.GetComponent, but that's just
        //           slow and not elegant at all, it should be passed to ForEach)
        //       2. InterestManagement systems have an
        //          [UpdateBefore(NetworkBroadcastSystem)] attribute, which
        //          would not work for a generic class.
        //       3. Some systems have special sync rules, like sync-to-owner or
        //          SyncDirection like NetworkTransform, so we can't assume that
        //          all components will be sent to all observers.
        //          In fact, this way we can have custom party member sync etc.
        protected abstract void Broadcast();

        // broadcast every send 'interval' seconds
        protected override void OnUpdate()
        {
            if (Time.ElapsedTime >= lastTime + interval)
            {
                Broadcast();
                lastTime = Time.ElapsedTime;
            }
        }
    }
}
