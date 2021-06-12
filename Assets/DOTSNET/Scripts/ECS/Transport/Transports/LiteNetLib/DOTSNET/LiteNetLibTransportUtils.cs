using System;
using LiteNetLib;

namespace DOTSNET.LiteNetLib
{
    public static class LiteNetLibTransportUtils
    {
        public static DeliveryMethod ConvertChannel(Channel channel)
        {
            switch (channel)
            {
                case Channel.Reliable:
                    return DeliveryMethod.ReliableOrdered;
                case Channel.Unreliable:
                    return DeliveryMethod.Unreliable;
                default:
                    throw new Exception("Unexpected channel: " + channel);
            }
        }
    }
}