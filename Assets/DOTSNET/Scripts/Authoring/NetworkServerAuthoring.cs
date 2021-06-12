// Modify NetworkServer settings via Authoring.
// -> all functions are virtual in case we want to inherit!

using System;
using UnityEngine;

namespace DOTSNET
{
    public class NetworkServerAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        // find NetworkServerSystem in ECS world
        NetworkServerSystem server =>
            Bootstrap.ServerWorld.GetExistingSystem<NetworkServerSystem>();

        // add system if Authoring is used
        public virtual Type GetSystemType() { return typeof(NetworkServerSystem); }

        // grab state from ECS world
        public ServerState state => server.state;

        // configuration
        public bool startIfHeadless = true;
        public float tickRate = 60;
        public int connectionLimit = 1000;

        // apply configuration in Awake already
        // doing it in StartServer is TOO LATE because ECS world might auto
        // start the server in headless mode, in which case the authoring
        // StartServer function would never be called and the configuration
        // would never be applied.
        protected virtual void Awake()
        {
            server.startIfHeadless = startIfHeadless;
            server.tickRate = tickRate;
            server.connectionLimit = connectionLimit;
        }

        // call StartServer in ECS world
        public virtual void StartServer()
        {
            Debug.Log("Calling StartServer in ECS World...");
            server.StartServer();
        }

        // forward StopServer request to ECS world
        public virtual void StopServer()
        {
            Debug.Log("Calling StopServer in ECS World...");
            server.StopServer();
        }
    }
}