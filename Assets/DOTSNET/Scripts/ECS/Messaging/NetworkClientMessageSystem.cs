// helper class to inherit from for message processing.
// * .client access for ease of use
// * [ClientWorld] tag already specified
// * RegisterMessage + Handler already set up
using Unity.Entities;
using UnityEngine;

namespace DOTSNET
{
    [ClientWorld]
    [UpdateInGroup(typeof(ClientConnectedSimulationSystemGroup))]
    public abstract class NetworkClientMessageSystem<T> : SystemBase
        where T : unmanaged, NetworkMessage
    {
        // dependencies
        [AutoAssign] protected NetworkClientSystem client;

        // the handler function
        protected abstract void OnMessage(T message);

        // messages NEED to be registered in OnCreate.
        // we are in the ConnectedSimulationSystemGroup, so OnStartRunning would
        // only be called after connecting, at which point we might already have
        // received a message of type T before setting up the handler.
        // (not using ConnectedGroup wouldn't be ideal. we don't want to do any
        //  message processing unless connected.)
        protected override void OnCreate()
        {
            // register handler
            if (client.RegisterHandler<T>(OnMessage))
            {
                Debug.Log("NetworkClientMessage/System Registered for: " + typeof(T));
            }
            else Debug.LogError("NetworkClientMessageSystem: failed to register handler for: " + typeof(T) + ". Was a handler for that message type already registered?");
        }

        // OnDestroy unregisters the message
        // Otherwise OnCreate can't register it again without an error,
        // and we really do want to have a legitimate error there in case
        // someone accidentally registers two handlers for one message.
        protected override void OnDestroy()
        {
            client.UnregisterHandler<T>();
        }
    }
}