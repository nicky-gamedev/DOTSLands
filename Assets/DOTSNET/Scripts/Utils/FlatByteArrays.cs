// helper class to copy Unity.Collections.Bytes30 (etc.) into byte[]
// -> ArraySegments can use those functions too!
using Unity.Collections;

namespace DOTSNET
{
    public static class FlatByteArrays
    {
        // copy Bytes16 struct to byte[]
        public static bool Bytes16ToArray(Bytes16 value, byte[] array, int arrayOffset)
        {
            // enough space in array?
            // => check total size before any writes to make it atomic!
            if (array != null &&
                arrayOffset + 16 <= array.Length)
            {
                // copy the 16 bytes
                array[arrayOffset] = value.byte0000;
                array[arrayOffset + 1] = value.byte0001;
                array[arrayOffset + 2] = value.byte0002;
                array[arrayOffset + 3] = value.byte0003;
                array[arrayOffset + 4] = value.byte0004;
                array[arrayOffset + 5] = value.byte0005;
                array[arrayOffset + 6] = value.byte0006;
                array[arrayOffset + 7] = value.byte0007;
                array[arrayOffset + 8] = value.byte0008;
                array[arrayOffset + 9] = value.byte0009;
                array[arrayOffset + 10] = value.byte0010;
                array[arrayOffset + 11] = value.byte0011;
                array[arrayOffset + 12] = value.byte0012;
                array[arrayOffset + 13] = value.byte0013;
                array[arrayOffset + 14] = value.byte0014;
                array[arrayOffset + 15] = value.byte0015;

                // success
                return true;
            }
            // not enough space
            return false;
        }

        // copy Bytes30 struct to byte[]
        public static bool Bytes30ToArray(Bytes30 value, byte[] array, int arrayOffset)
        {
            // enough space in array?
            // => check total size before any writes to make it atomic!
            if (array != null &&
                arrayOffset + 30 <= array.Length)
            {
                // copy the first 14 bytes
                array[arrayOffset] = value.byte0000;
                array[arrayOffset + 1] = value.byte0001;
                array[arrayOffset + 2] = value.byte0002;
                array[arrayOffset + 3] = value.byte0003;
                array[arrayOffset + 4] = value.byte0004;
                array[arrayOffset + 5] = value.byte0005;
                array[arrayOffset + 6] = value.byte0006;
                array[arrayOffset + 7] = value.byte0007;
                array[arrayOffset + 8] = value.byte0008;
                array[arrayOffset + 9] = value.byte0009;
                array[arrayOffset + 10] = value.byte0010;
                array[arrayOffset + 11] = value.byte0011;
                array[arrayOffset + 12] = value.byte0012;
                array[arrayOffset + 13] = value.byte0013;

                // copy the last 16 bytes
                return Bytes16ToArray(value.byte0014, array, arrayOffset + 14);
            }
            // not enough space
            return false;
        }

        // copy Bytes62 struct to byte[]
        public static bool Bytes62ToArray(Bytes62 value, byte[] array, int arrayOffset)
        {
            // enough space in array?
            // => check total size before any writes to make it atomic!
            if (array != null &&
                arrayOffset + 62 <= array.Length)
            {
                // copy the first 14 bytes
                array[arrayOffset] = value.byte0000;
                array[arrayOffset + 1] = value.byte0001;
                array[arrayOffset + 2] = value.byte0002;
                array[arrayOffset + 3] = value.byte0003;
                array[arrayOffset + 4] = value.byte0004;
                array[arrayOffset + 5] = value.byte0005;
                array[arrayOffset + 6] = value.byte0006;
                array[arrayOffset + 7] = value.byte0007;
                array[arrayOffset + 8] = value.byte0008;
                array[arrayOffset + 9] = value.byte0009;
                array[arrayOffset + 10] = value.byte0010;
                array[arrayOffset + 11] = value.byte0011;
                array[arrayOffset + 12] = value.byte0012;
                array[arrayOffset + 13] = value.byte0013;

                // copy the last 48 bytes
                return Bytes16ToArray(value.byte0014, array, arrayOffset + 14) &&
                       Bytes16ToArray(value.byte0030, array, arrayOffset + 30) &&
                       Bytes16ToArray(value.byte0046, array, arrayOffset + 46);
            }
            // not enough space to write
            return false;
        }

        // copy Bytes126 struct to byte[]
        public static bool Bytes126ToArray(Bytes126 value, byte[] array, int arrayOffset)
        {
            // enough space in array?
            // => check total size before any writes to make it atomic!
            if (array != null &&
                arrayOffset + 126 <= array.Length)
            {
                // copy the first 14 bytes
                array[arrayOffset] = value.byte0000;
                array[arrayOffset + 1] = value.byte0001;
                array[arrayOffset + 2] = value.byte0002;
                array[arrayOffset + 3] = value.byte0003;
                array[arrayOffset + 4] = value.byte0004;
                array[arrayOffset + 5] = value.byte0005;
                array[arrayOffset + 6] = value.byte0006;
                array[arrayOffset + 7] = value.byte0007;
                array[arrayOffset + 8] = value.byte0008;
                array[arrayOffset + 9] = value.byte0009;
                array[arrayOffset + 10] = value.byte0010;
                array[arrayOffset + 11] = value.byte0011;
                array[arrayOffset + 12] = value.byte0012;
                array[arrayOffset + 13] = value.byte0013;

                // copy the last 112 bytes
                return Bytes16ToArray(value.byte0014, array, arrayOffset + 14) &&
                       Bytes16ToArray(value.byte0030, array, arrayOffset + 30) &&
                       Bytes16ToArray(value.byte0046, array, arrayOffset + 46) &&
                       Bytes16ToArray(value.byte0062, array, arrayOffset + 62) &&
                       Bytes16ToArray(value.byte0078, array, arrayOffset + 78) &&
                       Bytes16ToArray(value.byte0094, array, arrayOffset + 94) &&
                       Bytes16ToArray(value.byte0110, array, arrayOffset + 110);
            }
            // not enough space to write
            return false;
        }

        // copy Bytes510 struct to byte[]
        public static bool Bytes510ToArray(Bytes510 value, byte[] array, int arrayOffset)
        {
            // enough space in array?
            // => check total size before any writes to make it atomic!
            if (array != null &&
                arrayOffset + 510 <= array.Length)
            {
                // copy the first 14 bytes
                array[arrayOffset] = value.byte0000;
                array[arrayOffset + 1] = value.byte0001;
                array[arrayOffset + 2] = value.byte0002;
                array[arrayOffset + 3] = value.byte0003;
                array[arrayOffset + 4] = value.byte0004;
                array[arrayOffset + 5] = value.byte0005;
                array[arrayOffset + 6] = value.byte0006;
                array[arrayOffset + 7] = value.byte0007;
                array[arrayOffset + 8] = value.byte0008;
                array[arrayOffset + 9] = value.byte0009;
                array[arrayOffset + 10] = value.byte0010;
                array[arrayOffset + 11] = value.byte0011;
                array[arrayOffset + 12] = value.byte0012;
                array[arrayOffset + 13] = value.byte0013;

                // copy the last 496 bytes
                return Bytes16ToArray(value.byte0014, array, arrayOffset + 14) &&
                       Bytes16ToArray(value.byte0030, array, arrayOffset + 30) &&
                       Bytes16ToArray(value.byte0046, array, arrayOffset + 46) &&
                       Bytes16ToArray(value.byte0062, array, arrayOffset + 62) &&
                       Bytes16ToArray(value.byte0078, array, arrayOffset + 78) &&
                       Bytes16ToArray(value.byte0094, array, arrayOffset + 94) &&
                       Bytes16ToArray(value.byte0110, array, arrayOffset + 110) &&
                       Bytes16ToArray(value.byte0126, array, arrayOffset + 126) &&
                       Bytes16ToArray(value.byte0142, array, arrayOffset + 142) &&
                       Bytes16ToArray(value.byte0158, array, arrayOffset + 158) &&
                       Bytes16ToArray(value.byte0174, array, arrayOffset + 174) &&
                       Bytes16ToArray(value.byte0190, array, arrayOffset + 190) &&
                       Bytes16ToArray(value.byte0206, array, arrayOffset + 206) &&
                       Bytes16ToArray(value.byte0222, array, arrayOffset + 222) &&
                       Bytes16ToArray(value.byte0238, array, arrayOffset + 238) &&
                       Bytes16ToArray(value.byte0254, array, arrayOffset + 254) &&
                       Bytes16ToArray(value.byte0270, array, arrayOffset + 270) &&
                       Bytes16ToArray(value.byte0286, array, arrayOffset + 286) &&
                       Bytes16ToArray(value.byte0302, array, arrayOffset + 302) &&
                       Bytes16ToArray(value.byte0318, array, arrayOffset + 318) &&
                       Bytes16ToArray(value.byte0334, array, arrayOffset + 334) &&
                       Bytes16ToArray(value.byte0350, array, arrayOffset + 350) &&
                       Bytes16ToArray(value.byte0366, array, arrayOffset + 366) &&
                       Bytes16ToArray(value.byte0382, array, arrayOffset + 382) &&
                       Bytes16ToArray(value.byte0398, array, arrayOffset + 398) &&
                       Bytes16ToArray(value.byte0414, array, arrayOffset + 414) &&
                       Bytes16ToArray(value.byte0430, array, arrayOffset + 430) &&
                       Bytes16ToArray(value.byte0446, array, arrayOffset + 446) &&
                       Bytes16ToArray(value.byte0462, array, arrayOffset + 462) &&
                       Bytes16ToArray(value.byte0478, array, arrayOffset + 478) &&
                       Bytes16ToArray(value.byte0494, array, arrayOffset + 494);
            }
            // not enough space to write
            return false;
        }

        // create Bytes16 struct from byte[]
        public static bool ArrayToBytes16(byte[] array, int arrayOffset, out Bytes16 value)
        {
            // enough data to read?
            // => check total size before any reads to make it atomic!
            if (array != null && arrayOffset + 16 <= array.Length)
            {
                // read the 16 bytes
                value.byte0000 = array[arrayOffset];
                value.byte0001 = array[arrayOffset + 1];
                value.byte0002 = array[arrayOffset + 2];
                value.byte0003 = array[arrayOffset + 3];
                value.byte0004 = array[arrayOffset + 4];
                value.byte0005 = array[arrayOffset + 5];
                value.byte0006 = array[arrayOffset + 6];
                value.byte0007 = array[arrayOffset + 7];
                value.byte0008 = array[arrayOffset + 8];
                value.byte0009 = array[arrayOffset + 9];
                value.byte0010 = array[arrayOffset + 10];
                value.byte0011 = array[arrayOffset + 11];
                value.byte0012 = array[arrayOffset + 12];
                value.byte0013 = array[arrayOffset + 13];
                value.byte0014 = array[arrayOffset + 14];
                value.byte0015 = array[arrayOffset + 15];

                // success
                return true;
            }
            // not enough data to read
            value = new Bytes16();
            return false;
        }

        // create Bytes30 struct from byte[]
        public static bool ArrayToBytes30(byte[] array, int arrayOffset, out Bytes30 value)
        {
            // enough data to read?
            // => check total size before any reads to make it atomic!
            if (array != null && arrayOffset + 30 <= array.Length)
            {
                // read the first 14 bytes
                value.byte0000 = array[arrayOffset];
                value.byte0001 = array[arrayOffset + 1];
                value.byte0002 = array[arrayOffset + 2];
                value.byte0003 = array[arrayOffset + 3];
                value.byte0004 = array[arrayOffset + 4];
                value.byte0005 = array[arrayOffset + 5];
                value.byte0006 = array[arrayOffset + 6];
                value.byte0007 = array[arrayOffset + 7];
                value.byte0008 = array[arrayOffset + 8];
                value.byte0009 = array[arrayOffset + 9];
                value.byte0010 = array[arrayOffset + 10];
                value.byte0011 = array[arrayOffset + 11];
                value.byte0012 = array[arrayOffset + 12];
                value.byte0013 = array[arrayOffset + 13];

                // read the last 16 bytes
                return ArrayToBytes16(array, arrayOffset + 14, out value.byte0014);
            }
            value = new Bytes30();
            return false;
        }

        // create Bytes62 struct from byte[]
        public static bool ArrayToBytes62(byte[] array, int arrayOffset, out Bytes62 value)
        {
            // enough data to read?
            // => check total size before any reads to make it atomic!
            if (array != null && arrayOffset + 62 <= array.Length)
            {
                // read the first 14 bytes
                value.byte0000 = array[arrayOffset];
                value.byte0001 = array[arrayOffset + 1];
                value.byte0002 = array[arrayOffset + 2];
                value.byte0003 = array[arrayOffset + 3];
                value.byte0004 = array[arrayOffset + 4];
                value.byte0005 = array[arrayOffset + 5];
                value.byte0006 = array[arrayOffset + 6];
                value.byte0007 = array[arrayOffset + 7];
                value.byte0008 = array[arrayOffset + 8];
                value.byte0009 = array[arrayOffset + 9];
                value.byte0010 = array[arrayOffset + 10];
                value.byte0011 = array[arrayOffset + 11];
                value.byte0012 = array[arrayOffset + 12];
                value.byte0013 = array[arrayOffset + 13];

                // read the last 3 x 16 bytes
                if (ArrayToBytes16(array, arrayOffset + 14, out value.byte0014) &&
                    ArrayToBytes16(array, arrayOffset + 30, out value.byte0030) &&
                    ArrayToBytes16(array, arrayOffset + 46, out value.byte0046))
                    return true;
            }
            value = new Bytes62();
            return false;
        }

        // create Bytes126 struct from byte[]
        public static bool ArrayToBytes126(byte[] array, int arrayOffset, out Bytes126 value)
        {
            // enough data to read?
            // => check total size before any reads to make it atomic!
            if (array != null && arrayOffset + 126 <= array.Length)
            {
                // read the first 14 bytes
                value.byte0000 = array[arrayOffset];
                value.byte0001 = array[arrayOffset + 1];
                value.byte0002 = array[arrayOffset + 2];
                value.byte0003 = array[arrayOffset + 3];
                value.byte0004 = array[arrayOffset + 4];
                value.byte0005 = array[arrayOffset + 5];
                value.byte0006 = array[arrayOffset + 6];
                value.byte0007 = array[arrayOffset + 7];
                value.byte0008 = array[arrayOffset + 8];
                value.byte0009 = array[arrayOffset + 9];
                value.byte0010 = array[arrayOffset + 10];
                value.byte0011 = array[arrayOffset + 11];
                value.byte0012 = array[arrayOffset + 12];
                value.byte0013 = array[arrayOffset + 13];

                // read the last 7 x 16 bytes
                if (ArrayToBytes16(array, arrayOffset + 14, out value.byte0014) &&
                    ArrayToBytes16(array, arrayOffset + 30, out value.byte0030) &&
                    ArrayToBytes16(array, arrayOffset + 46, out value.byte0046) &&
                    ArrayToBytes16(array, arrayOffset + 62, out value.byte0062) &&
                    ArrayToBytes16(array, arrayOffset + 78, out value.byte0078) &&
                    ArrayToBytes16(array, arrayOffset + 94, out value.byte0094) &&
                    ArrayToBytes16(array, arrayOffset + 110, out value.byte0110))
                    return true;
            }
            value = new Bytes126();
            return false;
        }

        // create Bytes510 struct from byte[]
        public static bool ArrayToBytes510(byte[] array, int arrayOffset, out Bytes510 value)
        {
            // enough data to read?
            // => check total size before any reads to make it atomic!
            if (array != null && arrayOffset + 510 <= array.Length)
            {
                // read the first 14 bytes
                value.byte0000 = array[arrayOffset];
                value.byte0001 = array[arrayOffset + 1];
                value.byte0002 = array[arrayOffset + 2];
                value.byte0003 = array[arrayOffset + 3];
                value.byte0004 = array[arrayOffset + 4];
                value.byte0005 = array[arrayOffset + 5];
                value.byte0006 = array[arrayOffset + 6];
                value.byte0007 = array[arrayOffset + 7];
                value.byte0008 = array[arrayOffset + 8];
                value.byte0009 = array[arrayOffset + 9];
                value.byte0010 = array[arrayOffset + 10];
                value.byte0011 = array[arrayOffset + 11];
                value.byte0012 = array[arrayOffset + 12];
                value.byte0013 = array[arrayOffset + 13];

                // read the last 31 x 16 bytes
                if (ArrayToBytes16(array, arrayOffset + 14, out value.byte0014) &&
                    ArrayToBytes16(array, arrayOffset + 30, out value.byte0030) &&
                    ArrayToBytes16(array, arrayOffset + 46, out value.byte0046) &&
                    ArrayToBytes16(array, arrayOffset + 62, out value.byte0062) &&
                    ArrayToBytes16(array, arrayOffset + 78, out value.byte0078) &&
                    ArrayToBytes16(array, arrayOffset + 94, out value.byte0094) &&
                    ArrayToBytes16(array, arrayOffset + 110, out value.byte0110) &&
                    ArrayToBytes16(array, arrayOffset + 126, out value.byte0126) &&
                    ArrayToBytes16(array, arrayOffset + 142, out value.byte0142) &&
                    ArrayToBytes16(array, arrayOffset + 158, out value.byte0158) &&
                    ArrayToBytes16(array, arrayOffset + 174, out value.byte0174) &&
                    ArrayToBytes16(array, arrayOffset + 190, out value.byte0190) &&
                    ArrayToBytes16(array, arrayOffset + 206, out value.byte0206) &&
                    ArrayToBytes16(array, arrayOffset + 222, out value.byte0222) &&
                    ArrayToBytes16(array, arrayOffset + 238, out value.byte0238) &&
                    ArrayToBytes16(array, arrayOffset + 254, out value.byte0254) &&
                    ArrayToBytes16(array, arrayOffset + 270, out value.byte0270) &&
                    ArrayToBytes16(array, arrayOffset + 286, out value.byte0286) &&
                    ArrayToBytes16(array, arrayOffset + 302, out value.byte0302) &&
                    ArrayToBytes16(array, arrayOffset + 318, out value.byte0318) &&
                    ArrayToBytes16(array, arrayOffset + 334, out value.byte0334) &&
                    ArrayToBytes16(array, arrayOffset + 350, out value.byte0350) &&
                    ArrayToBytes16(array, arrayOffset + 366, out value.byte0366) &&
                    ArrayToBytes16(array, arrayOffset + 382, out value.byte0382) &&
                    ArrayToBytes16(array, arrayOffset + 398, out value.byte0398) &&
                    ArrayToBytes16(array, arrayOffset + 414, out value.byte0414) &&
                    ArrayToBytes16(array, arrayOffset + 430, out value.byte0430) &&
                    ArrayToBytes16(array, arrayOffset + 446, out value.byte0446) &&
                    ArrayToBytes16(array, arrayOffset + 462, out value.byte0462) &&
                    ArrayToBytes16(array, arrayOffset + 478, out value.byte0478) &&
                    ArrayToBytes16(array, arrayOffset + 494, out value.byte0494))
                    return true;
            }
            value = new Bytes510();
            return false;
        }
    }
}