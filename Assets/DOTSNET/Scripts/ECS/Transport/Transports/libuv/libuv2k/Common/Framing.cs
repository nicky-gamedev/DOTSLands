// libuv / TCP is stream based.
// we add framing (aka <length:4, payload:length>) messages ourselves.
using System;

namespace libuv2k
{
    public static class Framing
    {
        // fast int to byte[] conversion and vice versa
        // -> test with 100k conversions:
        //    BitConverter.GetBytes(ushort): 144ms
        //    bit shifting: 11ms
        // -> 10x speed improvement makes this optimization actually worth it
        // -> this way we don't need to allocate BinaryWriter/Reader either
        // -> 4 bytes because some people may want to send messages larger than
        //    64K bytes
        // -> big endian is standard for network transmissions, and necessary
        //    for compatibility with Erlang
        // -> non-alloc is important for MMO scale networking performance.
        public static void IntToBytesBigEndianNonAlloc(int value, byte[] bytes)
        {
            bytes[0] = (byte)(value >> 24);
            bytes[1] = (byte)(value >> 16);
            bytes[2] = (byte)(value >> 8);
            bytes[3] = (byte)value;
        }

        public static int BytesToIntBigEndian(byte[] bytes, int offset)
        {
            return (bytes[offset + 0] << 24) |
                   (bytes[offset + 1] << 16) |
                   (bytes[offset + 2] << 8) |
                    bytes[offset + 3];
        }

        // copy size header, data into payload buffer so we only have to do ONE
        // libuv QueueWriteStream call, not two.
        public static bool Frame(byte[] payload, ArraySegment<byte> message)
        {
            // enough space?
            if (payload.Length >= message.Count + 4)
            {
                // construct header (size) without allocations
                IntToBytesBigEndianNonAlloc(message.Count, payload);

                // copy data into it, starting at '4' after header
                Buffer.BlockCopy(message.Array, message.Offset, payload, 4, message.Count);
                return true;
            }
            return false;
        }

        // copy stream into message buffer and extract messages
        // -> this needs to be called only once.
        // -> OnMessage might be called multiple times, e.g. if stream contains
        //    10 messages
        // -> returns true while stream is still valid
        //    returns false if invalid data / header attacks occured
        //
        // NOTE: could reduce amount of memcpy if we used a framing buffer of
        //       size MaxMessageSize (from last time) + ReceiveBufferSize (from
        //       this time), then copied full recv buffer into it, then move
        //       along it at position. but that's significantly more complex.
        public static bool Unframe(byte[] framingBuffer, ref int framingPosition, ArraySegment<byte> stream, int MaxMessageSize, Action<ArraySegment<byte>> OnMessage)
        {
            // note: we adjust the stream ArraySegment after using bytes from it
            //       so 'stream' is always what's left in stream.

            // don't return before the full stream was processed
            while (stream.Count > 0)
            {
                // fill 4 bytes header if not full yet
                if (framingPosition < 4)
                {
                    // how many bytes do we still need for header?
                    int headerRemaining = 4 - framingPosition;

                    // copy up to 'headerRemaining' into framing buffer
                    int copy = Math.Min(headerRemaining, stream.Count);
                    Buffer.BlockCopy(stream.Array, stream.Offset, framingBuffer, framingPosition, copy);

                    // update framing position and stream
                    framingPosition += copy;
                    stream = new ArraySegment<byte>(stream.Array, stream.Offset + copy, stream.Count - copy);
                }

                // do we have enough in framing buffer to read header now?
                if (framingPosition >= 4)
                {
                    // read header
                    int size = BytesToIntBigEndian(framingBuffer, 0);

                    // protect against all kinds of attacks by making sure that
                    // header size is within MaxMessageSize
                    if (0 <= size && size <= MaxMessageSize)
                    {
                        // how many bytes do we still need for the rest of the message?
                        // we need 'size' and we have 'framingPosition' - 4 header
                        int messageRemaining = size - (framingPosition - 4);

                        // copy more bytes into it if needed
                        if (messageRemaining > 0)
                        {
                            // copy up to 'messageRemaining' into framing buffer
                            int copy = Math.Min(messageRemaining, stream.Count);
                            Buffer.BlockCopy(stream.Array, stream.Offset, framingBuffer, framingPosition, copy);

                            // update framing position and stream
                            framingPosition += copy;
                            stream = new ArraySegment<byte>(stream.Array, stream.Offset + copy, stream.Count - copy);
                        }

                        // do we have the full message now?
                        if (framingPosition - 4 == size)
                        {
                            // point an ArraySegment to it
                            ArraySegment<byte> message = new ArraySegment<byte>(framingBuffer, 4, size);

                            // call OnMessage
                            OnMessage(message);

                            // reset framing position to start
                            framingPosition = 0;

                            // don't return. let the loop try for next message.
                        }
                        // otherwise we need to wait for more data
                        else return true;
                    }
                    // some kind of attack. return false to indicate that the
                    // connection should be kicked.
                    else return false;
                }
                // otherwise there is nothing more to do
                else return true;
            }

            // we processed the full stream
            return true;
        }
    }
}