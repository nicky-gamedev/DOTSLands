// use a Bootstrap to separate client and server worlds in memory, instead of
// putting everything into the default world.
// -> fully separating client and server in different worlds allows for proper
//    memory separated host mode. in fact, there is not even special case for
//    host mode in DOTSNET. it's simply client and server running at the same
//    time.
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

namespace DOTSNET
{
    public class Bootstrap : ICustomBootstrap
    {
        // world names
        public const string ServerWorldName = "ServerWorld";
        public const string ClientWorldName = "ClientWorld";

        // worlds for easier access. this way we don't need to search by name.
        public static World DefaultWorld;
        public static World ServerWorld;
        public static World ClientWorld;

        // initialize is called by Unity
        public bool Initialize(string defaultWorldName)
        {
            // fix: initialize TypeManager before using types.
            // this seems to be necessary in development builds and on some
            // systems, otherwise people get this error:
            // "The TypeManager must be initialized before the TypeManager can be used"
            TypeManager.Initialize();

            // create all worlds
            Debug.Log("DOTSNET Boostrap: creating Worlds");
            CreateAllWorlds(defaultWorldName, out DefaultWorld, out ServerWorld, out ClientWorld);

            // set default injection world and start updating
            World.DefaultGameObjectInjectionWorld = DefaultWorld;
            ScriptBehaviourUpdateOrder.AddWorldToCurrentPlayerLoop(DefaultWorld);

            // make sure that runInBackground is enabled.
            EnableRunInBackground();

            // success
            return true;
        }

        // check if a system has an attribute
        public static bool SystemHasAttribute(Type system, Type attribute)
        {
            return TypeManager.GetSystemAttributes(system, attribute)?.Length > 0;
        }

        // otherwise a client would time out if the window is minimized for
        // a minute or two.
        // and a server/host would stop updating the game.
        public static void EnableRunInBackground()
        {
            if (!Application.runInBackground)
            {
                Application.runInBackground = true;
                Debug.Log("DOTSNET: runInBackground enabled!");
            }
        }

        // categorize default systems into defaultUnity/defaultUser/server/client
        //   * unity: what Unity adds by default, like WorldTimeSystem
        //   * regular: user added but without [ServerWorld] etc. attributes
        //   * server: the ones with [ServerWorld] attribute
        //   * client: the ones with [ClientWorld] attribute
        public static void CategorizeSystems(IReadOnlyList<Type> systems, out List<Type> unitySystems, out List<Type> regularSystems, out List<Type> serverSystems, out List<Type> clientSystems)
        {
            // create the lists
            unitySystems = new List<Type>();
            regularSystems = new List<Type>();
            serverSystems = new List<Type>();
            clientSystems = new List<Type>();

            // loop through all systems
            foreach (Type system in systems)
            {
                // [ServerWorld] and [ClientWorld]?
                if (SystemHasAttribute(system, typeof(ServerWorldAttribute)) &&
                    SystemHasAttribute(system, typeof(ClientWorldAttribute)))
                {
                    serverSystems.Add(system);
                    clientSystems.Add(system);
                }
                // [ServerWorld]?
                else if (SystemHasAttribute(system, typeof(ServerWorldAttribute)))// && !isHybridRendererV2)
                {
                    serverSystems.Add(system);
                }
                // [ClientWorld]?
                else if (SystemHasAttribute(system, typeof(ClientWorldAttribute)))// || isHybridRendererV2)
                {
                    clientSystems.Add(system);
                }
#if ENABLE_HYBRID_RENDERER_V2
                // Hybrid Renderer V2 can only be in one world, not multiple.
                // otherwise we get 'material was registered twice' errors.
                else if (system.Namespace != null && system.Namespace.StartsWith("Unity.Rendering"))
                {
                    clientSystems.Add(system);
                }
#endif
                // no tag, and in Unity namespace?
                else if (system.Namespace != null && system.Namespace.StartsWith("Unity"))
                {
                    unitySystems.Add(system);
                }
                // no tag, and in Companion namespace?
                // this is needed for some Hybrid systems, see also:
                // https://github.com/vis2k/DOTSNET/issues/10
                // (they don't have a namespace, they just contain 'Companion')
                else if (system.Name.Contains("Companion"))
                {
                    unitySystems.Add(system);
                }
                // otherwise it's a third party asset, or one of our systems
                // that doesn't have a [Client/ServerWorld] attribute
                else
                {
                    regularSystems.Add(system);
                }
            }
        }

        // create the DefaultWorld with all Unity and all regular systems that
        // don't have a [Server/ClientWorld] attribute
        public static World CreateDefaultWorld(string defaultWorldName, List<Type> unitySystems, List<Type> regularSystems)
        {
            World defaultWorld = new World(defaultWorldName);

            // add unity systems
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(defaultWorld, unitySystems);
            // add regular systems with OnCreate dependency injection
            DependencyInjection.AddSystemsToRootLevelSystemGroupsAndInjectDependencies(defaultWorld, regularSystems);

            return defaultWorld;
        }

        // create the ServerWorld
        public static World CreateServerWorld(string worldName, List<Type> unitySystems, List<Type> serverSystems, World defaultWorld, bool addToDefaultUpdateList)
        {
            // create a new world
            World serverWorld = new World(worldName);

            // add unity systems
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(serverWorld, unitySystems);
            // add server systems with OnCreate dependency injection
            // -> no regular systems without [ServerWorld] attribute
            DependencyInjection.AddSystemsToRootLevelSystemGroupsAndInjectDependencies(serverWorld, serverSystems);

            // add hooks in DefaultWorld to update ServerWorld
            if (addToDefaultUpdateList)
            {
                InitializationSystemGroup initializationGroup = serverWorld.GetExistingSystem<InitializationSystemGroup>();
                defaultWorld.GetExistingSystem<InitializationSystemGroup>().AddSystemToUpdateList(initializationGroup);
                SimulationSystemGroup simulationGroup = serverWorld.GetExistingSystem<SimulationSystemGroup>();
                defaultWorld.GetExistingSystem<SimulationSystemGroup>().AddSystemToUpdateList(simulationGroup);
                // no rendering in ServerWorld
                //PresentationSystemGroup presentationGroup = clientWorld.GetExistingSystem<PresentationSystemGroup>();
                //defaultWorld.GetExistingSystem<PresentationSystemGroup>().AddSystemToUpdateList(presentationGroup);
            }

            // done
            Debug.Log("Created ServerWorld with " + serverSystems.Count + " systems");
            return serverWorld;
        }

        // create the ClientWorld
        public static World CreateClientWorld(string worldName, List<Type> unitySystems, List<Type> clientSystems, World defaultWorld, bool addToDefaultUpdateList)
        {
            // create a new world
            World clientWorld = new World(worldName);

            // add unity systems
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(clientWorld, unitySystems);
            // add client systems with OnCreate dependency injection
            // -> no regular systems without [ClientWorld] attribute
            DependencyInjection.AddSystemsToRootLevelSystemGroupsAndInjectDependencies(clientWorld, clientSystems);

            // add hooks in DefaultWorld to update ServerWorld
            if (addToDefaultUpdateList)
            {
                InitializationSystemGroup initializationGroup = clientWorld.GetExistingSystem<InitializationSystemGroup>();
                defaultWorld.GetExistingSystem<InitializationSystemGroup>().AddSystemToUpdateList(initializationGroup);
                SimulationSystemGroup simulationGroup = clientWorld.GetExistingSystem<SimulationSystemGroup>();
                defaultWorld.GetExistingSystem<SimulationSystemGroup>().AddSystemToUpdateList(simulationGroup);
                PresentationSystemGroup presentationGroup = clientWorld.GetExistingSystem<PresentationSystemGroup>();
                defaultWorld.GetExistingSystem<PresentationSystemGroup>().AddSystemToUpdateList(presentationGroup);
            }

            // done
            Debug.Log("Created ClientWorld with " + clientSystems.Count + " systems");
            return clientWorld;
        }

        // some systems like Transports have multiple implementations.
        // we use [DisableAutoCreation] to disable all by default and then add a
        // SelectiveSystemAuthoring component to the scene to enable it
        // selectively.
        // -> we need to call FindObjectsOfType to find all selective authoring
        //    components in the scene, but that's way better than having static
        //    state where the components register themselves into.
        // -> the only way for MonoBehaviours to register themselves before
        //    Bootstrap is called would be to use the MonoBehaviour constructor.
        //    but this gets called too unpredictably by Unity, and with domain
        //    reload disabled, a removed authoring system would still be in the
        //    static state list. it would be a mess.
        public static List<Type> GetAuthoringCreatedSystems()
        {
            // find all MonoBehaviours that implement the
            // SelectiveSystemAuthoring interface
            List<Type> systems = new List<Type>();
            foreach (MonoBehaviour component in GameObject.FindObjectsOfType<MonoBehaviour>())
            {
                if (component is SelectiveSystemAuthoring authoring)
                {
                    // get the system type that we want to create selectively
                    Type type = authoring.GetSystemType();

                    // safety check to make sure that all selective systems have
                    // [DisableAutoCreation] attribute
                    if (Attribute.IsDefined(type, typeof(DisableAutoCreationAttribute), false))
                    {
                        // add it
                        systems.Add(type);
                        //Debug.Log("Bootstrap: added Authoring selected system: " + type);
                    }
                    else Debug.LogError(component.GetType() + " is trying to selectively add " + type + " without [DisableAutoCreation] attribute");
                }
            }
            return systems;
        }

        // setup all worlds without updating anything yet.
        // this allows us to test this function.
        public static void CreateAllWorlds(string defaultWorldName, out World defaultWorld, out World serverWorld, out World clientWorld)
        {
            // get a list of all systems
            List<Type> allSystems = new List<Type>(DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default));

            // append selectively created systems
            allSystems.AddRange(GetAuthoringCreatedSystems());

            // categorize into unity/regular/server/client systems
            CategorizeSystems(allSystems, out List<Type> unitySystems, out List<Type> regularSystems, out List<Type> serverSystems, out List<Type> clientSystems);

            // create all worlds
            defaultWorld = CreateDefaultWorld(defaultWorldName, unitySystems, regularSystems);
            serverWorld = CreateServerWorld(ServerWorldName, unitySystems, serverSystems, defaultWorld, true);
            clientWorld = CreateClientWorld(ClientWorldName, unitySystems, clientSystems, defaultWorld, true);
        }
    }
}