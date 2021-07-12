using System;
using NUnit.Framework;
using Unity.Collections;

namespace Unity.Networking.Transport.Tests
{
    public class Base64Tests
    {
        static string FromBase64String(string base64)
        {
            unsafe
            {
                var maxLength = base64.Length / 4 * 3 + 2;
                var buffer = new byte[maxLength];

                fixed (byte* ptr = buffer)
                {
                    var actualLength = Base64.FromBase64String(base64, ptr, maxLength);
                    return new string((sbyte*) ptr, 0, actualLength);
                }
            }
        }

        static void Check(string normal, string base64)
        {
            var decoded = FromBase64String(base64);
            Assert.AreEqual(normal, decoded);
        }

        [Test]
        public void TestVector()
        {
            Check("", "");
            Check("f", "Zg==");
            Check("fo", "Zm8=");
            Check("foo", "Zm9v");
            Check("foob", "Zm9vYg==");
            Check("fooba", "Zm9vYmE=");
            Check("foobar", "Zm9vYmFy");
        }

#if !NET_DOTS
        private byte[] GenerateBinarySequence(Random rnd, int size)
        {
            var res = new byte[size];
            for (var i = 0; i < size; i++)
            {
                res[i] = (byte)rnd.Next(255);
            }
            return res;
        }

        [Test]
        public void TestRandomVector()
        {
            var rnd = new Random(513234124);
            const int n = 4096;
            var buffer = new byte[n];

            for (int i = 1; i < n; i++)
            {
                unsafe
                {
                    var seq = GenerateBinarySequence(rnd, i);
                    var base64String = Convert.ToBase64String(seq);

                    var correctBytes = Convert.FromBase64String(base64String);
                    UnityEngine.Assertions.Assert.AreEqual(i, correctBytes.Length);

                    for (int j = 0; j < i; j++)
                    {
                        UnityEngine.Assertions.Assert.AreEqual(seq[j], correctBytes[j]);
                    }

                    fixed (byte* ptr = buffer)
                    {
                        var actualLength = Base64.FromBase64String(base64String, ptr, i);

                        if (i != actualLength)
                        {
                            actualLength = Base64.FromBase64String(base64String, ptr, i);
                        }

                        UnityEngine.Assertions.Assert.AreEqual(i, actualLength);

                        for (int j = 0; j < i; j++)
                        {
                            UnityEngine.Assertions.Assert.AreEqual(seq[j], ptr[j]);
                        }
                    }
                }
            }
        }
#endif
    }
}