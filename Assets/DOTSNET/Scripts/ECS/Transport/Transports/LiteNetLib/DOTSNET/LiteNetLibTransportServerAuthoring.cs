using System;
using UnityEngine;

namespace DOTSNET.LiteNetLib
{
    public class LiteNetLibTransportServerAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        // find NetworkServerSystem in ECS world
        LiteNetLibTransportServerSystem server =>
            Bootstrap.ServerWorld.GetExistingSystem<LiteNetLibTransportServerSystem>();

        // common
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
        public Type GetSystemType() => typeof(LiteNetLibTransportServerSystem);

        // apply configuration in awake
        void Awake()
        {
            server.Port = Port;
            server.UpdateTime = UpdateTime;
            server.DisconnectTimeout = DisconnectTimeout;

            server.SimulateLatency = SimulateLatency;
            server.SimulationMinLatency = SimulationMinLatency;
            server.SimulationMaxLatency = SimulationMaxLatency;

            server.SimulatePacketLoss = SimulatePacketLoss;
            server.SimulationPacketLossChance = SimulationPacketLossChance;
        }

        /*void OnGUI()
        {
            if (GUI.Button(new Rect(10, 120, 100, 15), "SV SEND"))
            {
                server.Send(0, new ArraySegment<byte>(new byte[]{0x04, 0x05}));
            }
            if (GUI.Button(new Rect(120, 120, 160, 15), "SV DISCO CLIENT"))
            {
                server.Disconnect(0);
            }
        }*/
    }
}