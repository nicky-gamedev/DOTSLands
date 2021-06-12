// DOTSNET is limited by Socket send buffers. We need to compress data to get to
// the next level.
using Unity.Mathematics;

namespace DOTSNET
{
    public static class Compression
    {
        // quaternion compression //////////////////////////////////////////////
        // smallest three: https://gafferongames.com/post/snapshot_compression/
        // compresses 16 bytes quaternion into 4 bytes

        // helper function to find largest absolute element
        // returns the index of the largest one
        public static int LargestAbsoluteComponentIndex(float4 value, out float largest, out float3 withoutLargest)
        {
            // convert to abs
            float4 abs = math.abs(value);

            // set largest to first value (x)
            largest = value.x;
            withoutLargest = value.yzw;
            int index = 0;

            // compare to the others, starting at second value
            // performance for 100k calls
            //   for-loop:       25ms
            //   manual checks:  22ms
            if (abs.y > largest)
            {
                index = 1;
                largest = abs.y;
                withoutLargest = value.xzw;
            }
            if (abs.z > largest)
            {
                index = 2;
                largest = abs.z;
                withoutLargest = value.xyw;
            }
            if (abs.w > largest)
            {
                index = 3;
                largest = abs.w;
                withoutLargest = value.xyz;
            }

            return index;
        }

        // scale a float within min/max range to an ushort between min/max range
        // note: can also use this for byte range from byte.MinValue to byte.MaxValue
        public static ushort ScaleFloatToUShort(float value, float minValue, float maxValue, ushort minTarget, ushort maxTarget)
        {
            // note: C# ushort - ushort => int, hence so many casts
            // max ushort - min ushort only fits into something bigger
            int targetRange = maxTarget - minTarget;
            float valueRange = maxValue - minValue;
            float valueRelative = value - minValue;
            return (ushort)(minTarget + (ushort)(valueRelative / valueRange * targetRange));
        }

        // scale an ushort within min/max range to a float between min/max range
        // note: can also use this for byte range from byte.MinValue to byte.MaxValue
        public static float ScaleUShortToFloat(ushort value, ushort minValue, ushort maxValue, float minTarget, float maxTarget)
        {
            // note: C# ushort - ushort => int, hence so many casts
            float targetRange = maxTarget - minTarget;
            ushort valueRange = (ushort)(maxValue - minValue);
            ushort valueRelative = (ushort)(value - minValue);
            return minTarget + (valueRelative / (float)valueRange * targetRange);
        }

        const float QuaternionMinRange = -0.707107f;
        const float QuaternionMaxRange =  0.707107f;
        const ushort TenBitsMax = 0x3FF;

        // note: assumes normalized quaternions
        public static uint CompressQuaternion(quaternion q)
        {
            // note: assuming normalized quaternions is enough. no need to force
            //       normalize here. we already normalize when decompressing.

            // find the largest component index [0,3] + value
            int largestIndex = LargestAbsoluteComponentIndex(q.value, out float _, out float3 withoutLargest);

            // from here on, we work with the 3 components without largest!

            // "You might think you need to send a sign bit for [largest] in
            // case it is negative, but you don’t, because you can make
            // [largest] always positive by negating the entire quaternion if
            // [largest] is negative. in quaternion space (x,y,z,w) and
            // (-x,-y,-z,-w) represent the same rotation."
            if (q.value[largestIndex] < 0)
                withoutLargest = -withoutLargest;

            // put index & three floats into one integer.
            // => index is 2 bits (4 values require 2 bits to store them)
            // => the three floats are between [-0.707107,+0.707107] because:
            //    "If v is the absolute value of the largest quaternion
            //     component, the next largest possible component value occurs
            //     when two components have the same absolute value and the
            //     other two components are zero. The length of that quaternion
            //     (v,v,0,0) is 1, therefore v^2 + v^2 = 1, 2v^2 = 1,
            //     v = 1/sqrt(2). This means you can encode the smallest three
            //     components in [-0.707107,+0.707107] instead of [-1,+1] giving
            //     you more precision with the same number of bits."
            // => the article recommends storing each float in 9 bits
            // => our uint has 32 bits, so we might as well store in (32-2)/3=10
            //    10 bits max value: 1023=0x3FF (use OSX calc to flip 10 bits)
            ushort aScaled = ScaleFloatToUShort(withoutLargest.x, QuaternionMinRange, QuaternionMaxRange, 0, TenBitsMax);
            ushort bScaled = ScaleFloatToUShort(withoutLargest.y, QuaternionMinRange, QuaternionMaxRange, 0, TenBitsMax);
            ushort cScaled = ScaleFloatToUShort(withoutLargest.z, QuaternionMinRange, QuaternionMaxRange, 0, TenBitsMax);

            // now we just need to pack them into one integer
            // -> index is 2 bit and needs to be shifted to 31..32
            // -> a is 10 bit and needs to be shifted 20..30
            // -> b is 10 bit and needs to be shifted 10..20
            // -> c is 10 bit and needs to be at      0..10
            return (uint)(largestIndex << 30 | aScaled << 20 | bScaled << 10 | cScaled);
        }

        // note: gives normalized quaternions
        public static quaternion DecompressQuaternion(uint data)
        {
            // get cScaled which is at 0..10 and ignore the rest
            ushort cScaled = (ushort)(data & TenBitsMax);

            // get bScaled which is at 10..20 and ignore the rest
            ushort bScaled = (ushort)((data >> 10) & TenBitsMax);

            // get aScaled which is at 20..30 and ignore the rest
            ushort aScaled = (ushort)((data >> 20) & TenBitsMax);

            // get 2 bit largest index, which is at 31..32
            int largestIndex = (int)(data >> 30);

            // scale back to floats
            float a = ScaleUShortToFloat(aScaled, 0, TenBitsMax, QuaternionMinRange, QuaternionMaxRange);
            float b = ScaleUShortToFloat(bScaled, 0, TenBitsMax, QuaternionMinRange, QuaternionMaxRange);
            float c = ScaleUShortToFloat(cScaled, 0, TenBitsMax, QuaternionMinRange, QuaternionMaxRange);

            // calculate the omitted component based on a²+b²+c²+d²=1
            float d = math.sqrt(1 - a*a - b*b - c*c);

            // reconstruct based on largest index
            float4 value;
            switch (largestIndex)
            {
                case 0:  value = new float4(d, a, b, c); break;
                case 1:  value = new float4(a, d, b, c); break;
                case 2:  value = new float4(a, b, d, c); break;
                default: value = new float4(a, b, c, d); break;
            }

            // ECS Rotation only works with normalized quaternions.
            // make sure that's always the case here to avoid ECS bugs where
            // everything stops moving if the quaternion isn't normalized.
            return math.normalizesafe(new quaternion(value));
        }
    }
}