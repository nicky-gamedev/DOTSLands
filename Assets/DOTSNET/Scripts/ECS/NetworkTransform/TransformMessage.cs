// A message that synchronizes a NetworkEntity's position+rotation (=transform)
using Unity.Mathematics;

namespace DOTSNET
{
    public struct TransformMessage : NetworkMessage
    {
        // client needs to identify the entity by netId
        public ulong netId;

        // position
        public float3 position;

        // rotation is compressed from 16 bytes quaternion into 4 bytes
        //   100,000 messages * 16 byte = 1562 KB
        //   100,000 messages *  4 byte =  391 KB
        // => DOTSNET is transport limited, so this is a great idea.
        // => we serialize the message with a byte copy, so we need to already
        //    store the rotation as compressed.
        // => this is also easiest to use, since it's transparent to the user.
        uint compressedRotation;
        public quaternion rotation
        {
            get => Compression.DecompressQuaternion(compressedRotation);
            set => compressedRotation = Compression.CompressQuaternion(value);
        }

        public ushort GetID() { return 0x0025; }

        public TransformMessage(ulong netId, float3 position, quaternion rotation)
        {
            this.netId = netId;
            this.position = position;
            compressedRotation = 0;
            this.rotation = rotation;
        }
    }
}