// see https://revenantx.github.io/LiteNetLib/index.html for usage example
using System;
using Unity.Entities;
using UnityEngine;
using LiteNetLib;

namespace DOTSNET.LiteNetLib
{
    [ClientWorld]
    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class LiteNetLibTransportClientSystem : TransportClientSystem
    {
        // configuration
        public ushort Port = 8888;
        public int UpdateTime = 15;
        public int DisconnectTimeout = 5000;

        public bool SimulateLatency = false;
        public int SimulationMinLatency = 30;
        public int SimulationMaxLatency = 100;

        public bool SimulatePacketLoss = false;
        public int SimulationPacketLossChance = 10;

        // LiteNetLib state
        NetManager client;
        bool connected;

        public override bool Available()
        {
            // all except WebGL
            return Application.platform != RuntimePlatform.WebGLPlayer;
        }

        public override int GetMaxPacketSize()
        {
            // LiteNetLib NetPeer construct calls SetMTU(0), which sets it to
            // NetConstants.PossibleMtu[0] which is 576-68.
            // (bigger values will cause TooBigPacketException even on loopback)
            //
            // see also: https://github.com/RevenantX/LiteNetLib/issues/388
            return NetConstants.PossibleMtu[0];
        }

        public override bool IsConnected() => client != null && connected;

        public override void Connect(string address)
        {
            // not if already connected or connecting
            if (client != null)
            {
                Debug.LogWarning("LiteNet: client already connected/connecting.");
                return;
            }

            Debug.Log("LiteNet CL: connecting...");

            // create client
            EventBasedNetListener listener = new EventBasedNetListener();
            client = new NetManager(listener);
            client.UpdateTime = UpdateTime;
            client.DisconnectTimeout = DisconnectTimeout;
            client.IPv6Enabled = IPv6Mode.DualMode;
            client.SimulateLatency = SimulateLatency;
            client.SimulationMinLatency = SimulationMinLatency;
            client.SimulationMaxLatency = SimulationMaxLatency;
            client.SimulatePacketLoss = SimulatePacketLoss;
            client.SimulationPacketLossChance = SimulationPacketLossChance;

            // set up events
            listener.PeerConnectedEvent += peer =>
            {
                //Debug.Log("LiteNet CL client connected: " + peer.EndPoint);
                connected = true;
                OnConnected();
            };
            listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) =>
            {
                //Debug.Log("LiteNet CL received " + dataReader.AvailableBytes + " bytes. method=" + deliveryMethod);
                OnData(dataReader.GetRemainingBytesSegment());
                dataReader.Recycle();
            };
            listener.PeerDisconnectedEvent += (peer, info) =>
            {
                // this is called when the server stopped.
                // this is not called when the client disconnected.
                //Debug.Log("LiteNet CL disconnected. info=" + info);
                connected = false;
                OnDisconnected();
            };
            listener.NetworkErrorEvent += (point, error) =>
            {
                Debug.LogWarning("LiteNet CL network error: " + point + " error=" + error);
                // TODO should we disconnect or is it called automatically?
            };

            // start & connect
            client.Start();
            client.Connect(address, Port, "DOTSNET_LITENETLIB");
        }

        public override bool Send(ArraySegment<byte> segment, Channel channel)
        {
            if (client != null && client.FirstPeer != null)
            {
                try
                {
                    // convert DOTSNET channel to LiteNetLib channel & send
                    DeliveryMethod deliveryMethod = LiteNetLibTransportUtils.ConvertChannel(channel);
                    client.FirstPeer.Send(segment.Array, segment.Offset, segment.Count, deliveryMethod);
                    return true;
                }
                catch (TooBigPacketException exception)
                {
                    Debug.LogWarning("LiteNet CL: send failed. reason=" + exception);
                    return false;
                }
            }
            return false;
        }

        public override void Disconnect()
        {
            if (client != null)
            {
                // clean up
                client.Stop();
                client = null;
                connected = false;

                // PeerDisconnectedEvent is not called when voluntarily
                // disconnecting. need to call OnDisconnected manually.
                OnDisconnected();
            }
        }

        // ECS /////////////////////////////////////////////////////////////////
        protected override void OnUpdate()
        {
            // only if connected or connecting
            if (client != null)
            {
                client.PollEvents();
            }
        }
    }
}