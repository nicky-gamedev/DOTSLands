using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace DOTSNET
{
    public enum ServerState : byte
    {
        INACTIVE, ACTIVE
    }

    // NetworkMessage delegate for message handlers
    public delegate void NetworkMessageServerDelegate<T>(int connectionId, T message)
        where T : unmanaged, NetworkMessage;

    // message handler delegates are wrapped around another delegate because
    // the wrapping function still knows the message's type <T>, required for:
    //   1. Creating new T() before deserializing. we can't create a new
    //      NetworkMessage interface, only the explicit type.
    //   2. Knowing <T> when deserializing allows for automated serialization
    //      in the future.
    public delegate void NetworkMessageServerDelegateWrapper(int connectionId, SegmentReader reader);

    // NetworkServerSystem should be updated AFTER all other server systems.
    // we need a guaranteed update order to avoid race conditions where it might
    // randomly be updated before other systems, causing all kinds of unexpected
    // effects. determinism is always a good idea!
    // (this way NetworkServerMessageSystems can register handlers before
    //  OnStartRunning is called. remember that for all server systems,
    //  OnStartRunning is called the first time only after starting, so they
    //  absolutely NEED to be updated before this class, otherwise it would be
    //  impossible to register a ConnectMessage handler before
    //  OnTransportConnected is called (so the handler would never be called))
    [ServerWorld]
    // Server may need to apply physics, so update in the safe group
    [UpdateInGroup(typeof(ApplyPhysicsGroup))]
    [UpdateAfter(typeof(ServerActiveSimulationSystemGroup))]
    // use SelectiveAuthoring to create/inherit it selectively
    [DisableAutoCreation]
    public class NetworkServerSystem : SystemBase
    {
        // there is only one NetworkServer(System), so keep state in here
        // (using 1 component wouldn't gain us any performance. only complexity)

        // server state. we could use a bool, but we use a state for consistency
        // with NetworkClientSystem.state. it's more obvious this way.
        public ServerState state { get; private set; } = ServerState.INACTIVE;

        // dependencies
        [AutoAssign] protected PrefabSystem prefabSystem;
        [AutoAssign] protected InterestManagementSystem interestManagement;
        // transport is manually assign via FindAvailable
        public TransportServerSystem transport;

        // auto start the server in headless mode (= linux console)
        public bool startIfHeadless = true;

        // detect headless mode check, originally created for uMMORPG classic
        public readonly bool isHeadless =
            SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

        // tick rate for all ServerActiveSimulationSystemGroup systems in Hz.
        public float tickRate = 60;

        // connection limit. can be set at runtime to limit connections under
        // heavy load if needed.
        public int connectionLimit = 1000;

        // connections <connectionId, state>
        // we use a Dictionary instead of a NativeHashMap for now.
        // it's just easier to use, and we don't have to manually dispose every
        // connection state's owned objects.
        // and we can use it in foreach statements, unlike NativeHashMap.
        public Dictionary<int, ConnectionState> connections;

        // message handlers
        // -> we use delegates to be as widely usable as possible
        // -> KeyValuePair so that we can deserialize into a copy of Network-
        //    Message of the correct type, before calling the handler
        Dictionary<ushort, NetworkMessageServerDelegateWrapper> handlers =
            new Dictionary<ushort, NetworkMessageServerDelegateWrapper>();

        // all spawned NetworkEntities.
        // for cases where we need to modify one of them. this way we don't have
        // run a query over all of them.
        public Dictionary<ulong, Entity> spawned = new Dictionary<ulong, Entity>();

        // Send serializes messages into ArraySegments, which needs a byte[]
        // we use one for all sends.
        // we initialize it to transport maxpacketsize.
        // we buffer it to have allocation free sends via ArraySegments.
        // -> only use this from main thread!
        byte[] sendBuffer;

        // network control /////////////////////////////////////////////////////
        public void StartServer()
        {
            // do nothing if already started
            if (state == ServerState.ACTIVE)
                return;

            connections = new Dictionary<int, ConnectionState>(
                // use limit as first capacity. can still grow at runtime.
                connectionLimit
            );
            // start transport
            transport.Start();
            // set server to active AFTER starting transport to avoid potential
            // race conditions where someone might auto start a server when
            // active, assuming that transport was started too.
            state = ServerState.ACTIVE;

            // spawn scene objects
            SpawnSceneObjects();
        }

        public void StopServer()
        {
            // do nothing if already stopped
            if (state == ServerState.INACTIVE)
                return;

            // server solely operates on NetworkEntities.
            // destroy them all when stopping to clean up properly.
            // * StartServer spawns scene objects, we need to clean them before
            //   starting again, otherwise we have them 2x, 3x, etc.
            // * any accidental leftovers like projectiles should be cleaned too
            DestroyAllNetworkEntities();

            transport.Stop();
            state = ServerState.INACTIVE;
            // connections were 'null' before starting
            connections = null;
            spawned.Clear();
        }

        // disconnect a connection
        public void Disconnect(int connectionId)
        {
            // valid connectionId?
            if (connections.ContainsKey(connectionId))
            {
                // simply let transport disconnect it.
                // OnTransportDisconnect will handle the rest.
                transport.Disconnect(connectionId);
            }
        }

        // network events called by TransportSystem ////////////////////////////
        // named On'Transport'Connected etc. because OnServerConnected wouldn't
        // be completely obvious that it comes from the transport
        byte[] connectMessageBytes = new byte[Marshal.SizeOf<ConnectMessage>()];
        void OnTransportConnected(int connectionId)
        {
            Debug.Log("NetworkServerSystem.OnTransportConnected: " + connectionId);

            // connection limit not reached yet?
            if (connections.Count < connectionLimit)
            {
                // connection not added yet?
                if (!connections.ContainsKey(connectionId))
                {
                    // create a new connection state
                    ConnectionState connectionState = new ConnectionState();

                    // authenticate by default
                    // this may seem weird, but it's really smart way to support
                    // authenticators without NetworkServerSystem having to know
                    // about them:
                    // * Authenticators handle ConnectMessage
                    // * Immediately they set authenticated=false
                    // * Then they start authentication
                    //
                    // the alternative is to authenticate by default only if we
                    // don't have a ConnectMessage handler. but this would make
                    // too many assumptions about who hooks into ConnectMessage,
                    // and it would prevent anyone other than authenticators
                    // from hooking into ConnectMessage.
                    // we can assume that a lot projects will want to hook into
                    // ConnectMessage, so supporting authenticators by letting
                    // them overwrite authenticated state first is perfect.
                    connectionState.authenticated = true;

                    // add the connection
                    connections[connectionId] = connectionState;

                    // we only call OnConnect for classes that inherit from
                    // NetworkServerSystem, because they aren't in the
                    // ServerActiveSimulationSystemGroup and can't use
                    // OnStart/Stop Running to detect connect/disconnect.
                    OnConnected(connectionId);

                    // call OnConnect handler by invoking an artificial ConnectMessage
                    ConnectMessage message = new ConnectMessage();
                    if (handlers.TryGetValue(message.GetID(), out NetworkMessageServerDelegateWrapper handler))
                    {
                        // serialize connect message
                        SegmentWriter writer = new SegmentWriter(connectMessageBytes);
                        writer.WriteBlittable(message);

                        // handle it
                        handler(connectionId, new SegmentReader(writer.segment));
                        Debug.Log("NetworkServerSystem.OnTransportConnected: invoked ConnectMessage handler for connectionId: " + connectionId);
                    }
                }
                // otherwise reject it
                else
                {
                    Debug.Log("NetworkServerSystem: rejected connectionId: " + connectionId + " because already connected.");
                    transport.Disconnect(connectionId);
                }
            }
            // otherwise reject it
            else
            {
                Debug.Log("NetworkServerSystem: rejected connectionId: " + connectionId + " because limit reached.");
                transport.Disconnect(connectionId);
            }
        }

        // OnConnected callback for classes that inherit from NetworkServerSystem
        protected virtual void OnConnected(int connectionId) {}

        // segment's array is only valid until returning
        //
        // client->server protocol: <<messageId, message>>
        // (amount not worth it, clients usually send one message of type T)
        void OnTransportData(int connectionId, ArraySegment<byte> segment)
        {
            //Debug.Log("NetworkServerSystem.OnTransportData: " + connectionId + " => " + BitConverter.ToString(segment.Array, segment.Offset, segment.Count));

            // try to read the message id
            SegmentReader reader = new SegmentReader(segment);
            if (reader.ReadUShort(out ushort messageId))
            {
                //Debug.Log("NetworkServerSystem.OnTransportData messageId: 0x" + messageId.ToString("X4"));

                // create a new message of type messageId by copying the
                // template from the handler. we copy it automatically because
                // messages are value types, so that's a neat trick here.
                if (handlers.TryGetValue(messageId, out NetworkMessageServerDelegateWrapper handler))
                {
                    // deserialize and handle it
                    handler(connectionId, reader);
                }
                // unhandled messageIds are not okay. disconnect.
                else
                {
                    Debug.Log("NetworkServerSystem.OnTransportData: unhandled messageId: 0x" + messageId.ToString("X4") + " for connectionId: " + connectionId);
                    Disconnect(connectionId);
                }

            }
            // partial message ids are not okay. disconnect.
            else
            {
                Debug.Log("NetworkServerSystem.OnTransportData: failed to fully read messageId for segment with offset: " + segment.Offset + " length: " + segment.Count + " for connectionId: " + connectionId);
                Disconnect(connectionId);
            }
        }

        byte[] disconnectMessageBytes = new byte[Marshal.SizeOf<DisconnectMessage>()];
        void OnTransportDisconnected(int connectionId)
        {
            // call OnDisconnected handler by invoking an artificial DisconnectMessage
            // (in case a system needs it)
            DisconnectMessage message = new DisconnectMessage();
            if (handlers.TryGetValue(message.GetID(), out NetworkMessageServerDelegateWrapper handler))
            {
                // serialize disconnect message
                SegmentWriter writer = new SegmentWriter(disconnectMessageBytes);
                writer.WriteBlittable(message);

                // handle it
                handler(connectionId, new SegmentReader(writer.segment));
                Debug.Log("NetworkServerSystem.OnTransportDisconnected: invoked DisconnectMessage handler for connectionId: " + connectionId);
            }

            // we only call OnDisconnected for classes that inherit from
            // NetworkServerSystem, because they aren't in the
            // ServerActiveSimulationSystemGroup and can't use
            // OnStart/Stop Running to detect connect/disconnect.
            OnDisconnected(connectionId);

            // Unspawn all objects owned by that connection
            // => after DisconnectMessage handler because it might need to know
            //    about the player owned objects)
            // => before removing the connection, otherwise DestroyOwnedEntities
            //    can't look it up
            DestroyOwnedEntities(connectionId);

            // remove connectionId from connections
            connections.Remove(connectionId);
            Debug.Log("NetworkServerSystem.OnTransportDisconnected: " + connectionId);

            // interest management rebuild to remove the connectionId from all
            // entity's observers. otherwise broadcast systems will still try to
            // send to this connectionId, which isn't a big problem, but it does
            // give an "invalid connectionId" warning message.
            // and if we have 10k monsters, we get 10k warning messages, which
            // would actually slow down/freeze the editor for a short time.
            // => AFTER removing the connection, because RebuildAll will attempt
            //    to send unspawn messages, which have a connection-still-valid
            //    check.
            // NOTE: we could also use a simple removeFromAllObservers function,
            //       which would be faster, but it's also extra code and extra
            //       complexity. DOTS is really good at rebuilding observers.
            //       AND it wouldn't even work properly, because by just
            //       removing the connectionId, the observers would never get
            //       the unspawn messages.
            //       In other words, always do a full rebuild because IT WORKS
            //       perfectly and it's fast.
            // NOTE: even if we convert broadcast systems to jobs later, it's
            //       still going to work without race conditions because
            //       whatever system ends up sending the message queue, it will
            //       have to run from main thread because of IO
            interestManagement.RebuildAll();
        }

        // OnDisconnected callback for classes that inherit from NetworkServerSystem
        protected virtual void OnDisconnected(int connectionId) {}

        // messages ////////////////////////////////////////////////////////////
        // send a message to a connectionId over the specified channel.
        // (connectionId, message parameter order for consistency with transport
        //  and with Send(NativeMultiMap)
        //
        // server->client protocol: <<messageId:2, amount:4, messages:amount>>
        // (saves bandwidth, improves performance)
        public void Send<T>(int connectionId, T message, Channel channel = Channel.Reliable)
            where T : unmanaged, NetworkMessage
        {
            // valid connectionId?
            // Checking is technically not needed, but this way we get a nice
            // warning message and don't attempt a transport call with an
            // invalid connectionId, which is harder to debug because it simply
            // returns false in case of Apathy.
            if (connections.TryGetValue(connectionId, out ConnectionState connection))
            {
                // do nothing if the connection is broken.
                // we already logged a Send failed warning, and it will be
                // removed after the next transport update.
                // otherwise we might log 10k 'send failed' messages in-between
                // a failed send and the next transport update, which would
                // slow down the server and spam the logs.
                // -> for example, if we set the visibility radius very high in
                //    the 10k demo, DOTS will just send messages so fast that
                //    the Apathy transport buffers get full. this is to be
                //    expected, but previously the server would freeze for a few
                //    seconds because we logged thousands of "send failed"
                //    messages.
                if (!connection.broken)
                {
                    // make sure that we can use the send buffer
                    // (requires at least 6 bytes for header!)
                    if (sendBuffer?.Length > 6)
                    {
                        // create the segment writer
                        SegmentWriter writer = new SegmentWriter(sendBuffer);

                        // write message id & amount
                        if (writer.WriteUShort(message.GetID()) &&
                            writer.WriteUInt(1))
                        {
                            // serialize message content:
                            // instead of manually serializing every value via
                            // message.Serialize, we only allow blittable
                            // messages - which we can just block copy.
                            //
                            // that's a giant improvement:
                            // + no more need to write Serialize functions
                            // + no more accidentally forgetting a field
                            // + fastest performance: only one call, instead of
                            //   several WriteString, WriteInt, etc. calls and
                            //   checks
                            if (writer.WriteBlittable(message))
                            {
                                // send to transport.
                                // (it will have to free up the segment immediately)
                                if (!transport.Send(connectionId, writer.segment, channel))
                                {
                                    // send can fail if the transport has issues
                                    // like full buffers, broken pipes, etc.
                                    // so if Send gets called before the next
                                    // transport update removes the broken
                                    // connection, then we will see a warning.
                                    Debug.LogWarning("NetworkServerSystem.Send: failed to send message of type " + typeof(T) + " to connectionId: " + connectionId + ". This can happen if the connection is broken before the next transport update removes it. The connection has been flagged as broken and no more sends will be attempted for this connection until the next transport update cleans it up.");

                                    // if Send fails only once, we will flag the
                                    // connection as broken to avoid possibly
                                    // logging thousands of 'Send Message failed'
                                    // warnings in between the time send failed, and
                                    // transport update removes the connection.
                                    // it would just slow down the server
                                    // significantly, and spam the logs.
                                    connection.broken = true;

                                    // the transport is supposed to disconnect
                                    // the connection in case of send errors,
                                    // but to be 100% sure we call disconnect
                                    // here too.
                                    // we don't want a situation where broken
                                    // connections keep idling around because
                                    // the transport implementation forgot to
                                    // disconnect them.
                                    Disconnect(connectionId);
                                }
                            }
                            else Debug.LogWarning("NetworkServerSystem.Send: serializing message of type " + typeof(T) + " failed. Maybe the message is bigger than sendBuffer " + sendBuffer.Length + " bytes?");
                        }
                        else Debug.LogWarning("NetworkServerSystem.Send: writing message header of type " + typeof(T) + " failed. Maybe the id+amount is bigger than sendBuffer " + sendBuffer.Length + " bytes?");
                    }
                    else Debug.LogError("NetworkServerSystem.Send: sendBuffer not initialized or 0 length: " + sendBuffer);
                }
                // for debugging:
                //else Debug.Log("NetworkServerSystem.Send: skipped send to broken connectionId=" + connectionId);
            }
            else Debug.LogWarning("NetworkServerSystem.Send: invalid connectionId=" + connectionId);
        }

        // convenience function to send a whole NativeMultiMap of messages to
        // connections, useful for Job systems.
        //
        // PERFORMANCE: reusing Send(message, connectionId) is cleaner, but
        //              having the redundant code optimized to reuse writer and
        //              write messageId only once is significantly faster.
        //
        //              50k Entities @ no camera:
        //                              | NetworkTransformSystem ms | FPS
        //                reusing Send  |           6-11 ms         | 36-41 FPS
        //                optimized     |           4-8 ms          | 43-45 FPS
        //
        //              => NetworkTransformSystem runs almost twice as fast!
        //
        // DO NOT REUSE this function in Send(connectionId). this function here
        //              iterates all connections on the server. reusing would be
        //              slower.
        //
        // server->client protocol: <<messageId:2, amount:4, messages:amount>>
        // (saves bandwidth, improves performance)
        [Obsolete("Use Send(connectionId, NativeList) instead. See NetworkTransformServerSystem for example. This function might be removed soon.")]
        public void Send<T>(NativeMultiHashMap<int, T> messages, Channel channel = Channel.Reliable)
            where T : unmanaged, NetworkMessage
        {
            // make sure that we can use the send buffer
            // (requires at least 6 bytes for header!)
            if (sendBuffer?.Length > 6)
            {
                // create the segment writer only once
                SegmentWriter writer = new SegmentWriter(sendBuffer);

                // write message id & amount only once
                // TODO actually send more than one each time later
                if (writer.WriteUShort(new T().GetID()) &&
                    writer.WriteUInt(1))
                {
                    // reset writer position each time instead of creating a new
                    // one and writing the header again
                    int writerPosition = writer.Position;

                    // messages.GetKeyArray allocates.
                    // -> BroadcastSystems send to each connection anyway
                    // -> we need a connections.ContainsKey check anyway
                    // --> so we might as well iterate all known connections and only
                    //     send to the ones that are in messages (which are usually all)
                    foreach (KeyValuePair<int, ConnectionState> kvp in connections)
                    {
                        // unroll KeyValuePair for ease of use
                        int connectionId = kvp.Key;
                        ConnectionState connection = kvp.Value;

                        // do nothing if the connection is broken.
                        // we already logged a Send failed warning, and it will be
                        // removed after the next transport update.
                        // otherwise we might log 10k 'send failed' messages in-between
                        // a failed send and the next transport update, which would
                        // slow down the server and spam the logs.
                        // -> for example, if we set the visibility radius very high in
                        //    the 10k demo, DOTS will just send messages so fast that
                        //    the Apathy transport buffers get full. this is to be
                        //    expected, but previously the server would freeze for a few
                        //    seconds because we logged thousands of "send failed"
                        //    messages.
                        if (!connection.broken)
                        {
                            // iterate all messages for this connectionId
                            NativeMultiHashMapIterator<int>? it = default;
                            while (messages.TryIterate(connectionId, out T message, ref it))
                            {
                                // reset writer position to after message id
                                writer.Position = writerPosition;

                                // serialize message content:
                                // instead of manually serializing every value via
                                // message.Serialize, we only allow blittable
                                // messages - which we can just block copy.
                                //
                                // that's a giant improvement:
                                // + no more need to write Serialize functions
                                // + no more accidentally forgetting a field
                                // + fastest performance: only one call, instead of
                                //   several WriteString, WriteInt, etc. calls and
                                //   checks
                                if (writer.WriteBlittable(message))
                                {
                                    // send to transport.
                                    // (it will have to free up the segment immediately)
                                    if (!transport.Send(connectionId, writer.segment, channel))
                                    {
                                        // send can fail if the transport has issues
                                        // like full buffers, broken pipes, etc.
                                        // so if Send gets called before the next
                                        // transport update removes the broken
                                        // connection, then we will see a warning.
                                        Debug.LogWarning("NetworkServerSystem.Send: failed to send message of type " + typeof(T) + " to connectionId: " + connectionId + ". This can happen if the connection is broken before the next transport update removes it. The connection has been flagged as broken and no more sends will be attempted for this connection until the next transport update cleans it up.");

                                        // if Send fails only once, we will flag the
                                        // connection as broken to avoid possibly
                                        // logging thousands of 'Send Message failed'
                                        // warnings in between the time send failed, and
                                        // transport update removes the connection.
                                        // it would just slow down the server
                                        // significantly, and spam the logs.
                                        connection.broken = true;

                                        // the transport is supposed to disconnect
                                        // the connection in case of send errors,
                                        // but to be 100% sure we call disconnect
                                        // here too.
                                        // we don't want a situation where broken
                                        // connections keep idling around because
                                        // the transport implementation forgot to
                                        // disconnect them.
                                        Disconnect(connectionId);

                                        // no need to keep iterating through
                                        // messages for this connectionId.
                                        // we would just more warnings.
                                        break;
                                    }
                                }
                                else Debug.LogWarning("NetworkServerSystem.Send: serializing message of type " + typeof(T) + " failed. Maybe the message is bigger than sendBuffer " + sendBuffer.Length + " bytes?");
                            }
                        }
                        // for debugging:
                        //else Debug.Log("NetworkServerSystem.Send: skipped send to broken connectionId=" + connectionId);
                    }
                }
                else Debug.LogWarning("NetworkServerSystem.Send: writing message header of type " + typeof(T) + " failed. Maybe the header is bigger than sendBuffer " + sendBuffer.Length + " bytes?");
            }
            else Debug.LogError("NetworkServerSystem.Send: sendBuffer not initialized or 0 length: " + sendBuffer);
        }

        // chunk math
        public static int CalculateMessagesPerChunk(int bufferSize, int messageSize)
        {
            // what fits into payload after writing <<messageId:2, amount:4>> header?
            int spaceAfterHeader = bufferSize - 6;

            // so how many fit into one chunk?
            return spaceAfterHeader / messageSize;
        }

        // batch send messages to a connectionId.
        // => we send messages in MaxMessageSize chunks with <<messageId, amount, messages>
        // => DOTSNET automatically chunks them so Transports don't need to.
        //
        // benefits:
        // + save lots of bandwidth by only sending amount once
        // + free serialization: we just reinterpret the NativeList memory
        // + less transport.Send calls are always a good idea
        public void Send<T>(int connectionId, NativeList<T> messages, Channel channel = Channel.Reliable)
            where T : unmanaged, NetworkMessage
        {
            // do nothing if messages are empty. we don't want Transports to try
            // and send empty buffers.
            if (messages.Length == 0)
                return;

            // valid connectionId?
            // Checking is technically not needed, but this way we get a nice
            // warning message and don't attempt a transport call with an
            // invalid connectionId, which is harder to debug because it simply
            // returns false in case of Apathy.
            if (connections.TryGetValue(connectionId, out ConnectionState connection))
            {
                // do nothing if the connection is broken.
                // we already logged a Send failed warning, and it will be
                // removed after the next transport update.
                // otherwise we might log 10k 'send failed' messages in-between
                // a failed send and the next transport update, which would
                // slow down the server and spam the logs.
                // -> for example, if we set the visibility radius very high in
                //    the 10k demo, DOTS will just send messages so fast that
                //    the Apathy transport buffers get full. this is to be
                //    expected, but previously the server would freeze for a few
                //    seconds because we logged thousands of "send failed"
                //    messages.
                if (!connection.broken)
                {
                    // make sure that we can use the send buffer
                    // (requires at least 6 bytes for header!)
                    if (sendBuffer?.Length > 6)
                    {
                        // get the list's underlying buffer so we have free
                        // message serialization
                        NativeArray<T> array = messages.AsArray();

                        // create the segment writer
                        SegmentWriter writer = new SegmentWriter(sendBuffer);

                        // send in MaxMessageSize chunks.
                        // we can't just send the whole thing at once, because
                        // different transports support different messages sizes.
                        // -> TCP can pack large amounts into send buffer
                        // -> UDP can usually pack into MTU=1400 bytes
                        //    (which still allows for some smaller chunks!)

                        // how many messages can we put into each chunk?
                        // SegmentWriter.NativeArray uses UnsafeUtility.SizeOf,
                        // so we use it here too.
                        int messageSize = UnsafeUtility.SizeOf<T>();
                        int messagesPerChunk = CalculateMessagesPerChunk(sendBuffer.Length, messageSize);
                        if (messagesPerChunk > 0)
                        {
                            //Debug.Log("Sending " + math.ceil(messages.Length / (float)messagesPerChunk) + " chunks for type " + typeof(T));

                            // serialize all messages:
                            // instead of manually serializing every value via
                            // message.Serialize, we only allow blittable
                            // messages - which we can just block copy.
                            //
                            // that's a giant improvement:
                            // + no more need to write Serialize functions
                            // + no more accidentally forgetting a field
                            // + fastest performance: only one call, instead of
                            //   several WriteString, WriteInt, etc. calls and
                            //   checks
                            for (int i = 0; i < messages.Length; i+= messagesPerChunk)
                            {
                                // calculate amount of messages of this chunk
                                // => messagesPerChunk or whatever is remaining
                                int amount = math.min(messagesPerChunk, messages.Length - i);
                                //Debug.Log("Sending " + amount + "/" + messagesPerChunk + " messages of type " + typeof(T));

                                // reset writer
                                writer.Position = 0;

                                // put up to 'messagesPerChunk' messages into a chunk
                                // write message id, amount, chunk
                                if (writer.WriteUShort(new T().GetID()) &&
                                    writer.WriteUInt((uint)amount) &&
                                    writer.WriteNativeArray(array, i, amount))
                                {
                                    // send to transport.
                                    // (it will have to free up the segment immediately)
                                    if (!transport.Send(connectionId, writer.segment, channel))
                                    {
                                        // send can fail if the transport has issues
                                        // like full buffers, broken pipes, etc.
                                        // so if Send gets called before the next
                                        // transport update removes the broken
                                        // connection, then we will see a warning.
                                        Debug.LogWarning("NetworkServerSystem.Send: failed to send message of type " + typeof(T) + " to connectionId: " + connectionId + ". This can happen if the connection is broken before the next transport update removes it. The connection has been flagged as broken and no more sends will be attempted for this connection until the next transport update cleans it up.");

                                        // if Send fails only once, we will flag the
                                        // connection as broken to avoid possibly
                                        // logging thousands of 'Send Message failed'
                                        // warnings in between the time send failed, and
                                        // transport update removes the connection.
                                        // it would just slow down the server
                                        // significantly, and spam the logs.
                                        connection.broken = true;

                                        // the transport is supposed to disconnect
                                        // the connection in case of send errors,
                                        // but to be 100% sure we call disconnect
                                        // here too.
                                        // we don't want a situation where broken
                                        // connections keep idling around because
                                        // the transport implementation forgot to
                                        // disconnect them.
                                        Disconnect(connectionId);

                                        return;
                                    }
                                }
                                else
                                {
                                    Debug.LogWarning("NetworkServerSystem.Send: writing chunk of type " + typeof(T) + " failed. Maybe the id+amount+chunk is bigger than sendBuffer " + sendBuffer.Length + " bytes?");

                                    // if Send fails only once, we will flag the
                                    // connection as broken to avoid possibly
                                    // logging thousands of 'Send Message failed'
                                    // warnings in between the time send failed, and
                                    // transport update removes the connection.
                                    // it would just slow down the server
                                    // significantly, and spam the logs.
                                    connection.broken = true;

                                    // the transport is supposed to disconnect
                                    // the connection in case of send errors,
                                    // but to be 100% sure we call disconnect
                                    // here too.
                                    // we don't want a situation where broken
                                    // connections keep idling around because
                                    // the transport implementation forgot to
                                    // disconnect them.
                                    Disconnect(connectionId);

                                    return;
                                }
                            }
                        } else Debug.LogWarning("NetworkServerSystem.Send: send buffer size = " + sendBuffer.Length + " too small to pack even one message of type " + typeof(T) + " into a chunk.");
                    }
                    else Debug.LogError("NetworkServerSystem.Send: sendBuffer not initialized or 0 length: " + sendBuffer);
                }
                // for debugging:
                //else Debug.Log("NetworkServerSystem.Send: skipped send to broken connectionId=" + connectionId);
            }
            else Debug.LogWarning("NetworkServerSystem.Send: invalid connectionId=" + connectionId);
        }

        // we need to check authentication before calling handlers.
        // there are two options:
        // a) store 'requiresAuth' in the dictionary and check it before calling
        //    the handler each time.
        //    this works but we could accidentally forget checking requiresAuth.
        // b) wrap the handler in an requiresAuth check.
        //    this way we can never forget to call it.
        //    and we could add more code to the wrapper like message statistics.
        // => this is a bit of higher order function magic, but it's a really
        //    elegant solution.
        // => note that we lose the ability to compare handlers because we wrap
        //    them, but that's fine.
        NetworkMessageServerDelegateWrapper WrapHandler<T>(NetworkMessageServerDelegate<T> handler, bool requiresAuthentication)
            where T : unmanaged, NetworkMessage
        {
            return delegate(int connectionId, SegmentReader reader)
            {
                // find connection state
                if (connections.TryGetValue(connectionId, out ConnectionState state))
                {
                    // check authentication
                    // -> either we don't need it
                    // -> or if we need it, connection needs to be authenticated
                    if (!requiresAuthentication || state.authenticated)
                    {
                        // deserialize
                        // -> we do this in WrapHandler because in here we still
                        //    know <T>
                        // -> later on we only know NetworkMessage
                        // -> knowing <T> allows for automated serialization
                        if (reader.ReadBlittable(out T message))
                        {
                            // call it
                            handler(connectionId, message);
                        }
                        // invalid message contents are not okay. disconnect.
                        else
                        {
                            Debug.Log("NetworkServerSystem: failed to deserialize " + typeof(T) + " for reader with Position: " + reader.Position + " Remaining: " + reader.Remaining + " for connectionId: " + connectionId);
                            Disconnect(connectionId);
                        }
                    }
                    // authentication was required, but we were not authenticated
                    // in this case always disconnect the connection.
                    // no one is allowed to send unauthenticated messages.
                    else
                    {
                        Debug.Log("NetworkServerSystem: connectionId: " + connectionId + " disconnected because of unauthorized message.");
                        Disconnect(connectionId);
                    }
                }
                // this should not happen. if we try to call a handler for a
                // invalid connection then something went wrong.
                else
                {
                    Debug.LogError("NetworkServerSystem: connectionId: " + connectionId + " not found in WrapHandler. This should never happen.");
                }
            };
        }

        // register handler for a message.
        // we use 'where NetworkMessage' to make sure it only works for them,
        // and we use 'where new()' so we can create the type at runtime
        // => we use <T> generics so we don't have to pass both messageId and
        //    NetworkMessage template each time. it's just cleaner this way.
        //
        // usage: RegisterHandler<TestMessage>(func);
        public bool RegisterHandler<T>(NetworkMessageServerDelegate<T> handler, bool requiresAuthentication)
            where T : unmanaged, NetworkMessage
        {
            // create a message template to get id and to copy from
            T template = default;

            // make sure no one accidentally overwrites a handler
            // (might happen in case of duplicate messageIds etc.)
            if (!handlers.ContainsKey(template.GetID()))
            {
                // wrap the handler with auth check & deserialization
                handlers[template.GetID()] = WrapHandler(handler, requiresAuthentication);
                return true;
            }

            // log warning in case we tried to overwrite. could be extremely
            // useful for debugging/development, so we notice right away that
            // a system accidentally called it twice, or that two messages
            // accidentally have the same messageId.
            Debug.LogWarning("NetworkServerSystem: handler for " + typeof(T) + " was already registered.");
            return false;
        }

        // unregister a handler.
        // => we use <T> generics so we don't have to pass messageId  each time.
        public bool UnregisterHandler<T>()
            where T : unmanaged, NetworkMessage
        {
            // create a message template to get id
            T template = default;
            return handlers.Remove(template.GetID());
        }

        // spawn ///////////////////////////////////////////////////////////////
        // Spawn spawns an instantiated NetworkEntity prefab on all clients.
        // Always do it in this order:
        //   1. PrefabSystem.Get(prefabId)
        //   2. EntityManager.Instantiate(prefab)
        //   3. Set position and other custom data for a player/monster/etc.
        //   4. Spawn(prefab)
        //      -> sets netId
        //      -> sets ownerConnection
        //      -> etc.
        //
        // note: we pass an already instantiated NetworkEntity prefab instead of
        //       passing a prefabId and instantiating it in here.
        //       this is important because the caller might initialize a
        //       player's position, inventory, etc. before sending the spawn
        //       message to all clients.
        //       otherwise we would         spawn->modify->state update
        //       this way we        modify->spawn  without state update
        public bool Spawn(Entity entity, int? ownerConnectionId)
        {
            // only if server is active
            if (state != ServerState.ACTIVE)
                return false;

            // does it have a NetworkEntity component?
            if (HasComponent<NetworkEntity>(entity))
            {
                // was it not spawned yet? we can't spawn a monster/player twice
                NetworkEntity networkEntity = GetComponent<NetworkEntity>(entity);
                if (networkEntity.netId == 0)
                {
                    // set netId to Entity's unique id (Index + Version)
                    // there is no reason to generate a new unique id if we already
                    // have one. this is way easier to debug as well.
                    // -> on server, netId is simply the entity.UniqueId()
                    // -> on client, it's a unique Id from the server
                    networkEntity.netId = entity.UniqueId();

                    // set the owner connectionId
                    networkEntity.connectionId = ownerConnectionId;

                    // apply component changes
                    SetComponent(entity, networkEntity);

                    // if the Entity is owned by a connection, then add it to
                    // the connection's owned objects
                    if (ownerConnectionId != null)
                    {
                        // note: we don't have to reassign the struct because
                        // the ownedConnections property is just a pointer.
                        connections[ownerConnectionId.Value].ownedEntities.Add(entity);
                    }

                    // note: we don't rebuild observers after an Entity spawned.
                    //       this would cause INSANE complexity.
                    //       the next rebuild will detect new observers anyway.
                    //       (see InterestManagementSystem.RebuildAll comments)

                    // add to spawned
                    spawned[networkEntity.netId] = entity;

                    // success
                    //Debug.Log("NetworkServerSystem: Spawned Entity=" + EntityManager.GetName(entity) + " connectionId=" + ownerConnectionId);
                    return true;
                }
#if UNITY_EDITOR
                Debug.LogWarning("NetworkServerSystem.Spawn: can't spawn Entity=" + EntityManager.GetName(entity) + " prefabId=" + Conversion.Bytes16ToGuid(networkEntity.prefabId) + " connectionId=" + ownerConnectionId + " because the Entity was already spawned before with netId=" + networkEntity.netId);
#else
                Debug.LogWarning("NetworkServerSystem.Spawn: can't spawn Entity=" +                       entity  + " prefabId=" + Conversion.Bytes16ToGuid(networkEntity.prefabId) + " connectionId=" + ownerConnectionId + " because the Entity was already spawned before with netId=" + networkEntity.netId);
#endif
                return false;
            }
#if UNITY_EDITOR
            Debug.LogWarning("NetworkServerSystem.Spawn: can't spawn Entity=" + EntityManager.GetName(entity) + " connectionId=" + ownerConnectionId + " because the Entity has no NetworkEntity component.");
#else
            Debug.LogWarning("NetworkServerSystem.Spawn: can't spawn Entity=" +                       entity  + " connectionId=" + ownerConnectionId + " because the Entity has no NetworkEntity component.");
#endif
            return false;
        }

        // Unspawn should be used for all player owned objects after a player
        // disconnects, or after a monster dies, etc.
        // It broadcasts the despawn event to all connections so they remove it.
        //
        // IMPORTANT: Unspawn does NOT destroy the entity on the server.
        public bool Unspawn(Entity entity)
        {
            // only if server is active
            if (state != ServerState.ACTIVE)
                return false;

            // does it have a NetworkEntity component?
            if (HasComponent<NetworkEntity>(entity))
            {
                // was it spawned at all? we can't despawn an unspawned monster
                NetworkEntity networkEntity = GetComponent<NetworkEntity>(entity);
                if (networkEntity.netId != 0)
                {
                    // remove it from spawned by netId first, before we clear
                    // the netId
                    // -> checking the result of Remove is valuable to detect
                    //    unexpected cases.
                    if (spawned.Remove(networkEntity.netId))
                    {
                        // unspawning (& destroying) will fully remove the
                        // entity from the world. the observer system will never
                        // see it again, and never send any unspawn messages
                        // for this entity to all of the observers.
                        // we need to do it manually here.
                        DynamicBuffer<NetworkObserver> observers = GetBuffer<NetworkObserver>(entity);
                        for (int i = 0; i < observers.Length; ++i)
                        {
                            int observerConnectionId = observers[i];
#if UNITY_EDITOR
                            //Debug.Log("Unspawning " + EntityManager.GetName(entity) + " for observerConnectionId=" + observerConnectionId);
#else
                            //Debug.Log("Unspawning " +                       entity  + " for observerConnectionId=" + observerConnectionId);
#endif

                            // only if the connection still exists.
                            // we don't need to send an unspawn message to this connection if
                            // the observer was removed because the connection disconnected.
                            if (connections.ContainsKey(observerConnectionId))
                            {
                                // send it
                                Send(observerConnectionId, new UnspawnMessage(networkEntity.netId));
                            }
                        }

                        // note: we don't rebuild observers after an Entity
                        //       unspawned. this caused INSANE complexity.
                        //       the next rebuild will detect old observers anyway.
                        //       (see InterestManagementSystem.RebuildAll comments)

                        // clear netId because it's not spawned anymore
                        networkEntity.netId = 0;

                        // if the Entity is owned by a connection, then remove it
                        // from the connection's owned objects
                        if (networkEntity.connectionId != null)
                        {
                            // note: we don't have to reassign the struct because
                            // the ownedConnections property is just a pointer.
                            connections[networkEntity.connectionId.Value].ownedEntities.Remove(entity);
                        }

                        // clear owner
                        networkEntity.connectionId = null;

                        // apply component changes
                        SetComponent(entity, networkEntity);

                        // success
                        //Debug.Log("NetworkServerSystem: Unspawned Entity=" + EntityManager.GetName(entity));
                        return true;
                    }
#if UNITY_EDITOR
                    Debug.LogWarning("NetworkServerSystem.Unspawn: can't despawn Entity=" + EntityManager.GetName(entity) + " prefabId=" + Conversion.Bytes16ToGuid(networkEntity.prefabId) + " because an Entity with netId=" + networkEntity.netId + " was not spawned.");
#else
                    Debug.LogWarning("NetworkServerSystem.Unspawn: can't despawn Entity=" +                       entity  + " prefabId=" + Conversion.Bytes16ToGuid(networkEntity.prefabId) + " because an Entity with netId=" + networkEntity.netId + " was not spawned.");
#endif
                    return false;
                }
#if UNITY_EDITOR
                Debug.LogWarning("NetworkServerSystem.Unspawn: can't despawn Entity=" + EntityManager.GetName(entity) + " prefabId=" + Conversion.Bytes16ToGuid(networkEntity.prefabId) + " because netId is 0, so it was never spawned.");
#else
                Debug.LogWarning("NetworkServerSystem.Unspawn: can't despawn Entity=" +                       entity  + " prefabId=" + Conversion.Bytes16ToGuid(networkEntity.prefabId) + " because netId is 0, so it was never spawned.");
#endif
                return false;
            }
#if UNITY_EDITOR
            Debug.LogWarning("NetworkServerSystem.Unspawn: can't despawn Entity=" + EntityManager.GetName(entity) + " because the Entity has no NetworkEntity component.");
#else
            Debug.LogWarning("NetworkServerSystem.Unspawn: can't despawn Entity=" +                       entity  + " because the Entity has no NetworkEntity component.");
#endif
            return false;
        }

        // Destroy helper function that calls Unspawn, and then DestroyEntity.
        // in most (if not all) cases, we want to both unspawn and destroy.
        public void Destroy(Entity entity)
        {
            Unspawn(entity);
            EntityManager.DestroyEntity(entity);
        }

        // spawn all scene prefabs once.
        // we want them to be in the scene on start, which is why they were in
        // the MonoBehaviour scene/hierarchy.
        // -> PrefabSystem converts all scene entities to Prefabs when it starts
        // -> Server spawns instances of them when it starts
        //    -> this way we can still destroy/instantiate them again
        //    -> this way we make sure to assign a netId to them
        // note: we could make this the responsibility of the PrefabSystem in
        //       server world. but right now PrefabSystem is it's own standalone
        //       thing that doesn't know anything about the server. and that's
        //       good.
        void SpawnSceneObjects()
        {
            foreach (KeyValuePair<Bytes16, Entity> kvp in prefabSystem.scenePrefabs)
            {
                // instantiate
                Entity instance = EntityManager.Instantiate(kvp.Value);

                // spawn, but without rebuilding observers because there are
                // none yet.
                // (otherwise we would call RebuildObservers 10k if we have 10k
                //  scene objects)
                if (!Spawn(instance, null))
                {
#if UNITY_EDITOR
                    Debug.LogError("NetworkServerSystem.SpawnSceneObjects: failed to spawn scene object: " + EntityManager.GetName(kvp.Value) + " with prefabId=" + Conversion.Bytes16ToGuid(kvp.Key));
#else
                    Debug.LogError("NetworkServerSystem.SpawnSceneObjects: failed to spawn scene object: " +                       kvp.Value  + " with prefabId=" + Conversion.Bytes16ToGuid(kvp.Key));
#endif
                }
            }
        }

        // destroy all entities for a connectionId
        // public in case someone needs it (e.g. GM kicks) and for testing
        public void DestroyOwnedEntities(int connectionId)
        {
            // Unspawn all objects owned by that connection
            // (after DisconnectMessage handler because it might need to know
            //  about the player owned objects)
            if (connections.TryGetValue(connectionId, out ConnectionState connection))
            {
                // unspawn(so clients know about it) & destroy each entity
                // note: we need to duplicate the list for iteration, because
                //       Unspawn modifies it and would cause an exception while
                //       iterating. duplicating is the best solution because:
                //       * using a List means we could iterate backwards and
                //         remove from it at the same time, but then .Remove
                //         would be more costly than in a HashSet
                //       * passing a 'dontTouchOwnedEntities' to Unspawn would
                //         work and it would avoid allocations, but with the
                //         cost of extra complexity and a less elegant API
                //         because all of the sudden, there would be a strange
                //         parameter in Destroy/Unspawn.
                //       => players don't disconnect very often. it's okay to
                //          allocate here once.
                foreach (Entity entity in new HashSet<Entity>(connection.ownedEntities))
                {
                    Destroy(entity);
                }

                // note: no need to clear connection.ownedEntities because
                //       Unspawn already removes them from the list
            }
        }

        // destroy all NetworkEntities to clean up
        void DestroyAllNetworkEntities()
        {
            // IMPORTANT: EntityManager.DestroyEntity(query) does NOT work for
            //            Entities with LinkedEntityGroup. We would get an error
            //            about LinkedEntities needing to be destroyed first:
            //            https://github.com/vis2k/DOTSNET/issues/11
            //
            //            The solution is to use Destroy(NativeArray), which
            //            destroys linked entities too:
            //            https://forum.unity.com/threads/how-to-destroy-all-linked-entities.714890/#post-4777796
            //
            //            See NetworkServerSystemTests:
            //            StopDestroysAllNetworkEntitiesWithLinkedEntityGroup()
            EntityQuery networkEntities = GetEntityQuery(typeof(NetworkEntity));
            NativeArray<Entity> array = networkEntities.ToEntityArray(Allocator.TempJob);
            EntityManager.DestroyEntity(array);
            array.Dispose();
        }

        // join world //////////////////////////////////////////////////////////
        // the Join World process:
        //
        // -----Game Specific-----
        //   - Client selects a player
        //     - Client sends JoinWorld(playerPrefabId)
        //   - Server JoinWorldSystem validates selection
        //     - instantiates prefab
        //     - loads position, inventory
        // --------DOTSNET---------
        //   - NetworkServerSystem.JoinWorld(player)
        //     - marks connection as joined
        //     - Spawns player
        //
        // note: unlike Spawn, we pass connectionId first because that's what
        //       matters the most this time.
        // note: we do not handle spawn messages here. this is game specific and
        //       some might need a prefab index, a team, a map, etc.
        //       simply handle it in your game and call JoinWorld afterwards.
        public bool JoinWorld(int connectionId, Entity player)
        {
            // spawn the player on all clients, owned by the connection
            if (Spawn(player, connectionId))
            {
                // mark connection as 'joined world'. some systems might need it
                connections[connectionId].joinedWorld = true;

                // note: Spawn() rebuilds observers so everyone else knows about
                //       the new player, and the new player knows about everyone
                //       else. we don't need to do anything else here.
                return true;
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning("NetworkServerSystem.JoinWorld: failed to spawn player: " + EntityManager.GetName(player) + " for connectionId: " + connectionId);
#else
                Debug.LogWarning("NetworkServerSystem.JoinWorld: failed to spawn player: " +                       player  + " for connectionId: " + connectionId);
#endif
            }
            return false;
        }

        // component system ////////////////////////////////////////////////////
        // cache TransportSystem in OnStartRunning after all systems were created
        // (we can't assume that TransportSystem.OnCreate is created before this)
        protected override void OnStartRunning()
        {
            // find available server transport
            transport = TransportSystem.FindAvailable(World) as TransportServerSystem;
            if (transport != null)
            {
                // hook ourselves up to Transport events
                transport.OnConnected = OnTransportConnected;
                transport.OnData = OnTransportData;
                transport.OnDisconnected = OnTransportDisconnected;

                // initialize send buffer
                sendBuffer = new byte[transport.GetMaxPacketSize()];

                // do we have a simulation system group? then set tick rate.
                // (we won't have a group when running tests)
                ServerActiveSimulationSystemGroup group = World.GetExistingSystem<ServerActiveSimulationSystemGroup>();
                if (group != null)
                {
                    // CatchUp would be bad idea because it could lead to deadlocks
                    // under heavy load, where only the simulation system is updated
                    // so we use Simple.
                    float tickInterval = 1 / tickRate;
                    // TODO use EnableFixedRateSimple after it was fixed. right now
                    // it only sets deltaTime but updates with same max frequency
                    FixedRateUtils.EnableFixedRateWithCatchUp(group, tickInterval);
                    Debug.Log("NetworkServerSystem: " + group + " tick rate set to: " + tickRate);
                }

                // auto start in headless mode
                if (startIfHeadless && isHeadless)
                {
                    Debug.Log("NetworkServerSystem: automatically starting in headless mode...");
                    StartServer();
                }
            }
            else Debug.LogError("NetworkServerSystem: no available TransportServerSystem found on this platform: " + Application.platform);
        }

        protected override void OnUpdate()
        {
        }

        protected override void OnDestroy()
        {
            // stop server in case it was running
            StopServer();
        }
    }
}
