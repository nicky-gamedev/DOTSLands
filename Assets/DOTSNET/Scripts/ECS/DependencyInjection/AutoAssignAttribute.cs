// attribute for bootstrap dependency injection.
// [Dependency] is already taken by C#.
// [Inject] was used by ECS a while ago, and it sounds way too complicated for
//          users that don't know about dependency injection.
// [AutoAssign] is completely obvious.
using System;

namespace DOTSNET
{
    [AttributeUsage(AttributeTargets.Field)]
    public class AutoAssignAttribute : Attribute {}
}