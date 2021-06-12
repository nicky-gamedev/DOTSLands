// Modify NetworkClient settings via Authoring.
// -> all functions are virtual in case we want to inherit!
using System;
using UnityEngine;

namespace DOTSNET
{
    public class NetworkClientAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        // find NetworkClientSystem in ECS world
        NetworkClientSystem client =>
            Bootstrap.ClientWorld.GetExistingSystem<NetworkClientSystem>();

        // add system if Authoring is used
        public virtual Type GetSystemType() { return typeof(NetworkClientSystem); }

        // grab state from ECS world
        public ClientState state => client.state;

        // configuration
        public bool disconnectFreezesScene;

        // apply configuration in Awake
        protected virtual void Awake()
        {
            client.disconnectFreezesScene = disconnectFreezesScene;
        }

        // call Connect in ECS world
        public virtual void Connect(string address)
        {
            Debug.Log("Calling ClientConnect in ECS World...");
            client.Connect(address);
        }

        // forward Disconnect request to ECS world
        public virtual void Disconnect()
        {
            Debug.Log("Calling ClientDisconnect in ECS World...");
            client.Disconnect();
        }
    }
}