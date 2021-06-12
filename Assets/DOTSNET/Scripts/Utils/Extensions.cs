using System;
using Unity.Collections;
using Unity.Entities;

namespace DOTSNET
{
    public static class Extensions
    {
        // helper function to get a unique id for Entities.
        // it combines 4 bytes Index + 4 bytes Version into 8 bytes unique Id
        // note: in theory the Index should be enough because it is only reused
        //       after the Entity was destroyed. but let's be 100% safe and use
        //       Index + Version as recommended in the Entity documentation.
        public static ulong UniqueId(this Entity entity)
        {
            // convert to uint
            uint index = (uint)entity.Index;
            uint version = (uint)entity.Version;

            // shift version from 0x000000FFFFFFFF to 0xFFFFFFFF00000000
            ulong shiftedVersion = (ulong)version << 32;

            // OR into result
            return (index & 0xFFFFFFFF) | shiftedVersion;
        }

        // DynamicBuffer helper function to check if it contains an element
        public static bool Contains<T>(this DynamicBuffer<T> buffer, T value)
            where T : struct
        {
            // DynamicBuffer foreach allocates. use for.
            for (int i = 0; i < buffer.Length; ++i)
                // .Equals can't be called from a Job.
                // GetHashCode() works as long as <T> implements it manually!
                // (which is faster too!)
                if (buffer[i].GetHashCode() == value.GetHashCode())
                    return true;
            return false;
        }

        // NativeMultiMap has .GetValuesForKeyArray Enumerator, but using C#'s
        // 'foreach (...)' in Burst/Jobs causes an Invalid IL code exception:
        // https://forum.unity.com/threads/invalidprogramexception-invalid-il-code-because-of-ecs-generated-code-in-a-foreach-query.914387/
        // So we need our own iterator.
        //
        // Using .TryGetFirstValue + .TryGetNextValue works, but it's way too
        // cumbersome.
        //
        // This causes redundant code like 'Send()' in this example:
        //    if (messages.TryGetFirstValue(connectionId, out message,
        //        out NativeMultiHashMapIterator<int> it))
        //    {
        //        Send(message, connectionId);
        //        while (messages.TryGetNextValue(out message, ref it))
        //        {
        //            Send(message, connectionId);
        //        }
        //    }
        //
        // Making it really difficult to do more abstractions/optimizations.
        //
        // So let's create a helper function so it's easier to use:
        //    NativeMultiHashMapIterator<T>? iterator = default;
        //    while (messages.TryIterate(connectionId, out message, ref iterator))
        //    {
        //        Send(message, connectionId);
        //    }
        public static bool TryIterate<TKey, TValue>(
            this NativeMultiHashMap<TKey, TValue> map,
            TKey key,
            out TValue value,
            ref NativeMultiHashMapIterator<TKey>? it)
                where TKey : struct, IEquatable<TKey>
                where TValue : struct
        {
            // get first value if iterator not initialized yet & assign iterator
            if (!it.HasValue)
            {
                bool result = map.TryGetFirstValue(key, out value, out NativeMultiHashMapIterator<TKey> temp);
                it = temp;
                return result;
            }
            // otherwise get next value & assign iterator
            else
            {
                NativeMultiHashMapIterator<TKey> temp = it.Value;
                bool result = map.TryGetNextValue(out value, ref temp);
                it = temp;
                return result;
            }
        }
    }
}