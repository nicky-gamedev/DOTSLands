// component data for position + rotation (=transform) syncing.
// DOTSNET state updates don't sync the position / rotation themselves
// because it would overwrite local movement/prediction/rubberbanding/etc.
using Unity.Entities;

namespace DOTSNET
{
    //[GenerateAuthoringComponent] not auto generated. see authoring component.
    public struct NetworkTransform : IComponentData
    {
        // DOTSNET is server authoritative by default, but we can use client
        // authoritative movement for prototyping if needed.
        public SyncDirection syncDirection;
    }
}