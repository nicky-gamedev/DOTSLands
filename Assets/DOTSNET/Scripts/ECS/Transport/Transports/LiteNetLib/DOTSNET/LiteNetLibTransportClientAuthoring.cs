using System;
using UnityEngine;

namespace DOTSNET.LiteNetLib
{
    public class LiteNetLibTransportClientAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        // find NetworkClientSystem in ECS world
        LiteNetLibTransportClientSystem client =>
            Bootstrap.ClientWorld.GetExistingSystem<LiteNetLibTransportClientSystem>();

        // common
        [Header("Configuration")]
        public ushort Port = 8888;
        [Tooltip("Library logic update and send period in milliseconds")]
        public int UpdateTime = 15;
        [Tooltip("If NetManager doesn't receive any packet from remote peer during this time then connection will be closed")]
        public int DisconnectTimeout = 5000;

        [Header("Latency Simulation")]
        public bool SimulateLatency;
        public int SimulationMinLatency = 30;
        public int SimulationMaxLatency = 100;

        [Header("Packet Loss Simulation")]
        public bool SimulatePacketLoss;
        public int SimulationPacketLossChance = 10;

        // add to selectively created systems before Bootstrap is called
        public Type GetSystemType() => typeof(LiteNetLibTransportClientSystem);

        // apply configuration in awake
        void Awake()
        {
            client.Port = Port;
            client.UpdateTime = UpdateTime;
            client.DisconnectTimeout = DisconnectTimeout;

            client.SimulateLatency = SimulateLatency;
            client.SimulationMinLatency = SimulationMinLatency;
            client.SimulationMaxLatency = SimulationMaxLatency;

            client.SimulatePacketLoss = SimulatePacketLoss;
            client.SimulationPacketLossChance = SimulationPacketLossChance;
        }

        /*void OnGUI()
        {
            if (GUI.Button(new Rect(10, 100, 100, 15), "CL SEND"))
            {
                client.Send(new ArraySegment<byte>(new byte[]{0x01, 0x02}));
            }
        }*/
    }
}