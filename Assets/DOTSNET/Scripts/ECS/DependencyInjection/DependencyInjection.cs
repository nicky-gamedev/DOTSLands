// ECS having a custom Bootstrap class allows us to inject dependencies
// automatically.
//
// before:
//   class TestSystem : ComponentSystem
//   {
//       protected OtherSystem other;
//       protected override void OnStartRunning()
//       {
//           other = World.GetExistingSystem<OtherSystem>();
//       }
//   }
//
// after:
//   class TestSystem : ComponentSystem
//   {
//       [AutoAssign] protected OtherSystem other;
//   }
//
// => simply call InjectDependenciesInAllWorlds() once from Bootstrap!
// => this works for instance/static/public/private fields.
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Entities;
using UnityEngine;

namespace DOTSNET
{
    // the dependency injection class
    public static class DependencyInjection
    {
        // inject dependencies for all systems in a world
        public static void InjectDependencies(World world)
        {
            //Debug.Log("injecting for world: " + world.Name);
            // for each system
            foreach (ComponentSystemBase system in world.Systems)
            {
                // get the final type
                Type type = system.GetType();

                // for each field
                foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    // does it have [AutoAssign]?
                    if (field.IsDefined(typeof(AutoAssignAttribute), true))
                    {
                        // is there a system of that type in this world?
                        ComponentSystemBase dependency = world.GetExistingSystem(field.FieldType);
                        if (dependency != null)
                        {
                            field.SetValue(system, dependency);
                            //Debug.Log("Injected dependency for: " + type + "." + field.Name + " of type " + field.FieldType + " in world " + world.Name + " to " + dependency);
                        }
                        else Debug.LogWarning("Failed to [AutoAssign] " + type + "." + field.Name + " because the world " + world.Name + " has no system of type " + field.FieldType);
                    }
                }
            }
        }

        // DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups version
        // that adds a DependencyInjectionSystem to resolve dependencies in
        // OnCreate before any other system's OnCreate is called.
        public static void AddSystemsToRootLevelSystemGroupsAndInjectDependencies(
            World world, List<Type> systemTypes)
        {
            // prepend our DependencyInjectionSystem
            systemTypes.Insert(0, typeof(DependencyInjectionSystem));

            // add all systems
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systemTypes);
        }
    }

    // Bootstrap needs to add it as first system to create manually.
    // so don't automatically create it.
    [DisableAutoCreation]
    class DependencyInjectionSystem : SystemBase
    {
        // Bootstrap adds this system as the FIRST created system.
        // OnCreate is called before any other system's OnCreate.
        // this way, we can resolve all dependencies and all other systems can
        // already use them in OnCreate.
        //
        // the alternative was to call Bootstrap.InjectDependencies AFTER the
        // whole world was created, but each system's OnCreate is already called
        // when the world is created. so DependencyInjection wasn't available in
        // OnCreate before.
        protected override void OnCreate()
        {
            Debug.Log("Injecting Dependencies in World: " + World.Name);
            DependencyInjection.InjectDependencies(World);
        }

        protected override void OnUpdate() {}
    }
}
