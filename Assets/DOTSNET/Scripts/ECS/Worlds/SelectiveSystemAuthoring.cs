// some systems like Transports have multiple implementations.
// we use [DisableAutoCreation] to disable all by default and then add a
// SelectiveSystemAuthoring component to the scene to enable it selectively.
// => we use an interface so that we aren't dependent on UnityEngine in the ECS
//    folder at all.
using System;

namespace DOTSNET
{
    public interface SelectiveSystemAuthoring
    {
        Type GetSystemType();
    }
}
