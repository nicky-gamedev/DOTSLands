using System;
using Unity.Collections;

namespace DOTSNET
{
    public static class Conversion
    {
        // helper function to convert Guid to Bytes16
        public static Bytes16 GuidToBytes16(Guid guid)
        {
            byte[] byteArray = guid.ToByteArray();
            FlatByteArrays.ArrayToBytes16(byteArray, 0, out Bytes16 bytes);
            return bytes;
        }

        // helper function to convert Bytes16 to Guid
        public static Guid Bytes16ToGuid(Bytes16 bytes)
        {
            byte[] byteArray = new byte[16];
            FlatByteArrays.Bytes16ToArray(bytes, byteArray, 0);
            return new Guid(byteArray);
        }

        // convert an int to a Bytes16 Guid
        // (for sceneId to prefabId conversion, because prefabId is always
        //  Bytes16. the rest of the bytes are 0)
        public static Bytes16 ULongToBytes16(ulong value)
        {
            return new Bytes16
            {
                byte0000 = (byte)(value & 0xFF),
                byte0001 = (byte)((value >> 8) & 0xFF),
                byte0002 = (byte)((value >> 16) & 0xFF),
                byte0003 = (byte)((value >> 24) & 0xFF),
                byte0004 = (byte)((value >> 32) & 0xFF),
                byte0005 = (byte)((value >> 40) & 0xFF),
                byte0006 = (byte)((value >> 48) & 0xFF),
                byte0007 = (byte)((value >> 56) & 0xFF)
            };
        }
    }
}