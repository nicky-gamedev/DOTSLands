// SegmentReader reads blittable types from an ArraySegment.
//
// This way it's allocation free, doesn't need pooling, and doesn't need one
// extra abstraction.
//
// => Transport gives an ArraySegment, we use it all the way to the end.
// => should be compatible with DOTS Jobs/Burst because we use simple types!!!
// => this is also easily testable
// => all the functions return a bool to indicate if reading succeeded or not.
//    this way we can detect invalid messages / attacks easily
// => 100% safe from allocation attacks because WE DO NOT ALLOCATE ANYTHING.
//    if WriteBytesAndSize receives a uint.max header, we don't allocate giga-
//    bytes of RAM because we just return a segment of a segment.
// => only DOTS supported blittable types like float3, FixedString, etc.
//
// Endianness:
//   DOTSNET automatically serializes full structs.
//   this only works as long as all the platforms have the same endianness,
//   which is Little Endian on Mac/Windows/Linux.
//   (see https://www.coder.work/article/247983 in case we need both endians)
//
// Use C# extensions to add your own reader functions, for example:
//
//   public static bool ReadItem(this SegmentReader reader, out Item item)
//   {
//       return reader.ReadInt(out item.Id);
//   }
//
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace DOTSNET
{
    public struct SegmentReader
    {
        // the segment with Offset and Count
        // -> Offset is position in byte array
        // -> Count is bytes until the end of the segment
        ArraySegment<byte> segment;

        // previously we modified Offset & Count when reading.
        // now we have a separate Position that actually starts at '0', so based
        // on Offset
        // (previously we also recreated 'segment' after every read. now we just
        //  increase Position, which is easier and faster)
        public int Position;

        // helper field to calculate amount of bytes remaining to read
        // segment.Count is 'count from Offset', so we simply subtract Position
        // without subtracting Offset.
        // example:
        //   {0x00, 0x01, 0x02} and segment with offset = 1, count=2
        //   Remaining := 2 - 0 => 0
        //           (not 2 - offset => 2 - 1 => 1)
        public int Remaining => segment.Count - Position;

        public SegmentReader(ArraySegment<byte> segment)
        {
            this.segment = segment;
            Position = 0;
        }

        // read 'size' bytes for blittable(!) type T via fixed memory copying
        //
        // this works for all blittable structs, and the value order is always
        // the same on all platforms because:
        // "C#, Visual Basic, and C++ compilers apply the Sequential layout
        //  value to structures by default."
        // https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.structlayoutattribute?view=netcore-3.1
        public unsafe bool ReadBlittable<T>(out T value)
            where T : unmanaged
        {
            // check if blittable for safety.
            // calling this with non-blittable types like bool would otherwise
            // give us strange runtime errors.
            // (for example, 0xFF would be neither true/false in unit tests
            //  with Assert.That(value, Is.Equal(true/false))
            //
            // => it's enough to check in Editor
            // => the check is around 20% slower for 1mio reads
            // => it's definitely worth it to avoid strange non-blittable issues
#if UNITY_EDITOR
            if (!UnsafeUtility.IsBlittable(typeof(T)))
            {
                Debug.LogError(typeof(T) + " is not blittable!");
                value = default;
                return false;
            }
#endif

            // calculate size
            //   sizeof(T) gets the managed size at compile time.
            //   Marshal.SizeOf<T> gets the unmanaged size at runtime (slow).
            // => our 1mio writes benchmark is 6x slower with Marshal.SizeOf<T>
            // => for blittable types, sizeof(T) is even recommended:
            // https://docs.microsoft.com/en-us/dotnet/standard/native-interop/best-practices
            int size = sizeof(T);

            // enough data to read?
            if (segment.Array != null && Remaining >= size)
            {
                fixed (byte* ptr = &segment.Array[segment.Offset + Position])
                {
                    // Marshal class is 6x slower in our 10mio writes benchmark
                    //value = Marshal.PtrToStructure<T>((IntPtr)ptr);

                    // cast buffer to a T* pointer and then read from it.
                    // value is a copy of that memory.
                    // value does not live at 'ptr' position.
                    // we also have a unit test to guarantee that.
                    // (so changing Array does not change value afterwards)
                    // breakpoint here to check manually:
                    //void* valuePtr = UnsafeUtility.AddressOf(ref value);
                    value = *(T*)ptr;
                }
                Position += size;
                return true;
            }
            value = new T();
            return false;
        }

        // read 1 byte
        public bool ReadByte(out byte value) => ReadBlittable(out value);

        // read 1 byte boolean
        // Read"Bool" instead of "ReadBoolean" for consistency with ReadInt etc.
        public bool ReadBool(out bool value)
        {
            // read it as byte (which is blittable),
            // then convert to bool (which is not blittable)
            if (ReadByte(out byte temp))
            {
                value = temp != 0;
                return true;
            }
            value = false;
            return false;
        }

        // read 2 bytes ushort
        // Read"UShort" instead of "ReadUInt16" for consistency with ReadFloat etc.
        public bool ReadUShort(out ushort value) => ReadBlittable(out value);

        // read 2 bytes short
        // Read"Short" instead of "ReadInt16" for consistency with ReadFloat etc.
        public bool ReadShort(out short value) => ReadBlittable(out value);

        // read 4 bytes uint
        // Read"UInt" instead of "ReadUInt32" for consistency with ReadFloat etc.
        public bool ReadUInt(out uint value) => ReadBlittable(out value);

        // read 4 bytes int
        // Read"Int" instead of "ReadInt32" for consistency with ReadInt2 etc.
        public bool ReadInt(out int value) => ReadBlittable(out value);

        // peek 4 bytes int (read them without actually modifying the position)
        // -> this is useful for cases like ReadBytesAndSize where we need to
        //    peek the header first to decide if we do a full read or not
        //    (in other words, to make it atomic)
        // -> we pass segment by value, not by reference. this way we can reuse
        //    the regular ReadInt call without any modifications to segment.
        public bool PeekInt(out int value)
        {
            int previousPosition = Position;
            bool result = ReadInt(out value);
            Position = previousPosition;
            return result;
        }

        // read 8 bytes int2
        public bool ReadInt2(out int2 value) => ReadBlittable(out value);

        // read 12 bytes int3
        public bool ReadInt3(out int3 value) => ReadBlittable(out value);

        // read 16 bytes int4
        public bool ReadInt4(out int4 value) => ReadBlittable(out value);

        // read 8 bytes ulong
        // Read"ULong" instead of "ReadUInt64" for consistency with ReadFloat etc.
        public bool ReadULong(out ulong value) => ReadBlittable(out value);

        // read 8 bytes long
        // Read"Long" instead of "ReadInt64" for consistency with ReadFloat etc.
        public bool ReadLong(out long value) => ReadBlittable(out value);

        // read byte array as ArraySegment to avoid allocations
        public bool ReadBytes(int count, out ArraySegment<byte> value)
        {
            // enough data to read?
            // => check total size before any reads to make it atomic!
            if (segment.Array != null && Remaining >= count)
            {
                // create 'value' segment and point it at the right section
                value = new ArraySegment<byte>(segment.Array, segment.Offset + Position, count);

                // update position
                Position += count;
                return true;
            }
            // not enough data to read
            return false;
        }

        // read size, bytes as ArraySegment to avoid allocations
        public bool ReadBytesAndSize(out ArraySegment<byte> value)
        {
            // enough data to read?
            // => check total size before any reads to make it atomic!
            //    => at first it needs at least 4 bytes for the header
            //    => then it needs enough size for header + size bytes
            if (segment.Array != null && Remaining >= 4 &&
                PeekInt(out int size) &&
                0 <= size && 4 + size <= Remaining)
            {
                // we already peeked the size and it's valid. so let's skip it.
                Position += 4;

                // now do the actual bytes read
                // -> ReadBytes and ArraySegment constructor both use 'int', so we
                //    use 'int' here too. that's the max we can support. if we would
                //    use 'uint' then we would have to use a 'checked' conversion to
                //    int, which means that an attacker could trigger an Overflow-
                //    Exception. using int is big enough and fail safe.
                // -> ArraySegment.Array can't be null, so we don't have to
                //    handle that case
                return ReadBytes(size, out value);
            }
            // not enough data to read
            return false;
        }

        // read 4 bytes float
        // Read"Float" instead of ReadSingle for consistency with ReadFloat3 etc
        public bool ReadFloat(out float value) => ReadBlittable(out value);

        // read 8 bytes float2
        public bool ReadFloat2(out float2 value) => ReadBlittable(out value);

        // read 12 bytes float3
        public bool ReadFloat3(out float3 value) => ReadBlittable(out value);

        // read 16 bytes float4
        public bool ReadFloat4(out float4 value) => ReadBlittable(out value);

        // read 8 bytes double
        public bool ReadDouble(out double value) => ReadBlittable(out value);

        // read 16 bytes double2
        public bool ReadDouble2(out double2 value) => ReadBlittable(out value);

        // read 24 bytes double3
        public bool ReadDouble3(out double3 value) => ReadBlittable(out value);

        // read 32 bytes double4
        public bool ReadDouble4(out double4 value) => ReadBlittable(out value);

        // read 16 bytes decimal
        public bool ReadDecimal(out decimal value) => ReadBlittable(out value);

        // read 16 bytes quaternion
        public bool ReadQuaternion(out quaternion value) => ReadBlittable(out value);

        // read Bytes16 struct
        public bool ReadBytes16(out Bytes16 value) => ReadBlittable(out value);

        // read Bytes30 struct
        public bool ReadBytes30(out Bytes30 value) => ReadBlittable(out value);

        // read Bytes62 struct
        public bool ReadBytes62(out Bytes62 value) => ReadBlittable(out value);

        // read Bytes126 struct
        // => check total size before any reads to make it atomic!
        public bool ReadBytes126(out Bytes126 value) => ReadBlittable(out value);

        // read Bytes510 struct
        public bool ReadBytes510(out Bytes510 value) => ReadBlittable(out value);

        // read Bytes4094 struct
        public bool ReadBytes4094(out Bytes4094 value) => ReadBlittable(out value);

        // read FixedString32
        // -> fixed size means not worrying about max size / allocation attacks
        // -> fixed size saves size header
        public bool ReadFixedString32(out FixedString32 value) => ReadBlittable(out value);

        // read FixedString64
        // -> fixed size means not worrying about max size / allocation attacks
        // -> fixed size saves size header
        public bool ReadFixedString64(out FixedString64 value) => ReadBlittable(out value);

        // read FixedString128
        // -> fixed size means not worrying about max size / allocation attacks
        // -> fixed size saves size header
        public bool ReadFixedString128(out FixedString128 value) => ReadBlittable(out value);

        // read FixedString512
        // -> fixed size means not worrying about max size / allocation attacks
        // -> fixed size saves size header
        public bool ReadFixedString512(out FixedString512 value) => ReadBlittable(out value);
    }
}
