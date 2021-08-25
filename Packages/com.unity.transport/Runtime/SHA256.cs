using Unity.Collections;

namespace Unity.Networking.Transport
{
    public static class SHA256
    {
        internal unsafe struct SHA256State
        {
            public fixed uint state[8];
            public fixed byte buffer[64];
            private ulong count;

            public static SHA256State Create()
            {
                var result = new SHA256State();
                result.state[0] = 0x6a09e667;
                result.state[1] = 0xbb67ae85;
                result.state[2] = 0x3c6ef372;
                result.state[3] = 0xa54ff53a;
                result.state[4] = 0x510e527f;
                result.state[5] = 0x9b05688c;
                result.state[6] = 0x1f83d9ab;
                result.state[7] = 0x5be0cd19;
                return result;
            }

            public void Update(byte* data, int length)
            {
                var curBufferPos = count & 0x3F;
                while (length > 0)
                {
                    buffer[curBufferPos++] = *data++;
                    count++;
                    length--;
                    if (curBufferPos == 64)
                    {
                        curBufferPos = 0;
                        WriteByteBlock();
                    }
                }
            }

            public void Final(byte* dest)
            {
                var lenInBits = count << 3;
                var curBufferPos = (uint)(count & 0x3F);

                buffer[curBufferPos++] = 0x80;
                while (curBufferPos != 64 - 8)
                {
                    curBufferPos &= 0x3F;
                    if (curBufferPos == 0)
                        WriteByteBlock();

                    buffer[curBufferPos++] = 0;
                }

                for (var i = 0; i < 8; i++)
                {
                    buffer[curBufferPos++] = (byte)(lenInBits >> 56);
                    lenInBits <<= 8;
                }

                WriteByteBlock();

                for (var i = 0; i < 8; i++)
                {
                    *dest++ = (byte)(state[i] >> 24);
                    *dest++ = (byte)(state[i] >> 16);
                    *dest++ = (byte)(state[i] >> 8);
                    *dest++ = (byte)state[i];
                }
            }

            private void WriteByteBlock()
            {
                var data32 = stackalloc uint[16];
                for (var i = 0; i < 16; i++)
                    data32[i] =
                        ((uint)buffer[i * 4    ] << 24) +
                        ((uint)buffer[i * 4 + 1] << 16) +
                        ((uint)buffer[i * 4 + 2] <<  8) +
                        (uint)buffer[i * 4 + 3];
                Transform(data32);
            }

            static readonly uint[] K = {
                0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5,
                0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
                0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3,
                0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
                0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc,
                0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
                0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7,
                0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
                0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13,
                0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
                0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3,
                0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
                0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5,
                0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
                0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208,
                0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2
            };

            private void Transform(uint* data)
            {
                var W = stackalloc uint[16];
                var T = stackalloc uint[8];
                for (var j = 0; j < 8; j++)
                    T[j] = state[j];

                for (var j = 0; j < 64; j += 16)
                {
                    for (var i = 0; i < 16; i++)
                    {
                        T[(7-i)&7] += S1(T[(4-i)&7]) + Ch(T[(4-i)&7],T[(5-i)&7],T[(6-i)&7]) + K[i+j] + (j != 0 ? W[i&15] += s1(W[(i-2)&15]) + W[(i-7)&15] + s0(W[(i-15)&15]) : W[i] = data[i]);
                        T[(3-i)&7] += T[(7-i)&7];
                        T[(7-i)&7] += S0(T[(0-i)&7]) + Maj(T[(0-i)&7], T[(1-i)&7], T[(2-i)&7]);
                    }
                }

                for (var j = 0; j < 8; j++)
                    state[j] += T[j];

                static uint ROTR32(uint x, byte n) => (x << (32 - n)) | (x >> n);
                static uint S0(uint x) => ROTR32(x, 2) ^ ROTR32(x, 13) ^ ROTR32(x, 22);
                static uint S1(uint x) => ROTR32(x, 6) ^ ROTR32(x,11) ^ ROTR32(x, 25);
                static uint s0(uint x) => ROTR32(x, 7) ^ ROTR32(x,18) ^ (x >> 3);
                static uint s1(uint x) => ROTR32(x,17) ^ ROTR32(x,19) ^ (x >> 10);
                static uint Ch(uint x, uint y, uint z) => z^(x&(y^z));
                static uint Maj(uint x, uint y, uint z) => (x&y)|(z&(x|y));
            }
        }
    }
}