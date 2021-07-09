using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Assert = UnityEngine.Assertions.Assert;
using Random = System.Random;

namespace Unity.Networking.Transport.Tests
{
    public class HMACSHA256Tests
    {
        [Test]
        public void TestEmptyVectorSHA256()
        {
            unsafe
            {
                var str = (FixedString32) "";
                var result = new NativeArray<byte>(32, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

                Assert.AreEqual(0, str.Length);

                var sha256State = SHA256.SHA256State.Create();
                sha256State.Update(str.GetUnsafePtr(), str.Length);
                sha256State.Final((byte*) result.GetUnsafePtr());

#if !NET_DOTS
                ValidateWithReferenceImplementationSHA256(ToArray(str.ToString()), result);
#endif

                uint* r1 = (uint*) result.GetUnsafePtr();
                Assert.AreEqual(*r1++, 0x42c4b0e3);
                Assert.AreEqual(*r1++, 0x141cfc98);
                Assert.AreEqual(*r1++, 0xc8f4fb9a);
                Assert.AreEqual(*r1++, 0x24b96f99);
                Assert.AreEqual(*r1++, 0xe441ae27);
                Assert.AreEqual(*r1++, 0x4c939b64);
                Assert.AreEqual(*r1++, 0x1b9995a4);
                Assert.AreEqual(*r1, 0x55b85278);
            }
        }

        [Test]
        public void TestVectorSHA256()
        {
            unsafe
            {
                // from https://www.di-mgt.com.au/sha_testvectors.html

                // 896 bits
                var str = (FixedString512) "abcdefghbcdefghicdefghijdefghijkefghijklfghijklmghijklmnhijklmnoijklmnopjklmnopqklmnopqrlmnopqrsmnopqrstnopqrstu";
                var result = new NativeArray<byte>(32, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

                Assert.AreEqual(896 / 8, str.Length);

                var sha256State = SHA256.SHA256State.Create();
                sha256State.Update(str.GetUnsafePtr(), str.Length);
                sha256State.Final((byte*) result.GetUnsafePtr());

#if !NET_DOTS
                ValidateWithReferenceImplementationSHA256(ToArray(str.ToString()), result);
#endif

                uint* r1 = (uint*) result.GetUnsafePtr();
                Assert.AreEqual(*r1++, 0xa7165bcf);
                Assert.AreEqual(*r1++, 0x8083af78);
                Assert.AreEqual(*r1++, 0x9ee56c03);
                Assert.AreEqual(*r1++, 0x3792047b);
                Assert.AreEqual(*r1++, 0x119b240b);
                Assert.AreEqual(*r1++, 0x517af0e8);
                Assert.AreEqual(*r1++, 0x0345acaf);
                Assert.AreEqual(*r1, 0xd1e9fe7a);
            }
        }


        void ValidateHMACSHA256TestVector(byte[] key, byte[] message, ref FixedString128 expectedResult)
        {
            var expectedFromTestVector = StringToByteArray(expectedResult.ToString());

            var resultToValidate = new NativeArray<byte>(32, Allocator.Temp, NativeArrayOptions.UninitializedMemory);


            unsafe
            {
                fixed (byte* keyPtr = key)
                {
                    fixed (byte* messagePtr = message)
                    {
                        HMACSHA256.ComputeHash(keyPtr, key.Length,
                                               messagePtr, message.Length,
                                               (byte*) resultToValidate.GetUnsafePtr());
                    }
                }
            }

            AssertAreEqualSHA(expectedFromTestVector, resultToValidate, "Result is not the same as the test vector");

#if !NET_DOTS
            ValidateWithReferenceImplementationHMAC(key, message, resultToValidate);
#endif
        }

        static byte[] StringToByteArray(string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            var n = hex.Length / 2;
            var arr = new byte[n];

            for (var i = 0; i < n; ++i)
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + GetHexVal(hex[(i << 1) + 1]));

            return arr;
        }

        static int GetHexVal(char hex)
        {
            var val = (int)hex;
            //For uppercase A-F letters:
            //return val - (val < 58 ? 48 : 55);
            //For lowercase a-f letters:
            //return val - (val < 58 ? 48 : 87);
            //Or the two combined, but a bit slower:
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }

        // https://datatracker.ietf.org/doc/html/rfc4231#section-4.2
        [Test]
        public void TestVectorHMACSHA256_1()
        {
            FixedString512 key = "";
            for (var i = 0; i < 20; i++)
                key.Append((char)0x0b);
            FixedString512 message = "Hi There";
            FixedString128 expectedResult = "b0344c61d8db38535ca8afceaf0bf12b881dc200c9833da726e9376c2e32cff7";

            Assert.AreEqual(20, key.Length);

            ValidateHMACSHA256TestVector(ToArray(key.ToString()), ToArray(message.ToString()), ref expectedResult);
        }

        // https://datatracker.ietf.org/doc/html/rfc4231#section-4.2
        [Test]
        public void TestVectorHMACSHA256_2()
        {
            FixedString512 key = "Jefe";
            FixedString512 message = "what do ya want for nothing?";
            FixedString128 expectedResult = "5bdcc146bf60754e6a042426089575c75a003f089d2739839dec58b964ec3843";

            ValidateHMACSHA256TestVector(ToArray(key.ToString()), ToArray(message.ToString()), ref expectedResult);
        }

        // https://datatracker.ietf.org/doc/html/rfc4231#section-4.2
        [Test]
        public void TestVectorHMACSHA256_3()
        {
            var key = new NativeArray<byte>(20, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < 20; i++)
                key[i] = 0xaa;

            var message = new NativeArray<byte>(50, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < 50; i++)
                message[i] = 0xdd;

            FixedString128 expectedResult = "773ea91e36800e46854db8ebd09181a72959098b3ef8c122d9635514ced565fe";

            Assert.AreEqual(20, key.Length);
            Assert.AreEqual(50, message.Length);

            ValidateHMACSHA256TestVector(key.ToArray(), message.ToArray(), ref expectedResult);
        }

        // https://datatracker.ietf.org/doc/html/rfc4231#section-4.2
        [Test]
        public void TestVectorHMACSHA256_4()
        {
            var k = (byte) 0x01;
            var key = new NativeArray<byte>(25, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < 25; i++)
                key[i] = k++;

            var message = new NativeArray<byte>(50, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < 50; i++)
                message[i] = 0xcd;

            FixedString128 expectedResult = "82558a389a443c0ea4cc819899f2083a85f0faa3e578f8077a2e3ff46729665b";

            Assert.AreEqual(25, key.Length);
            Assert.AreEqual(50, message.Length);

            ValidateHMACSHA256TestVector(key.ToArray(), message.ToArray(), ref expectedResult);
        }

        // https://datatracker.ietf.org/doc/html/rfc4231#section-4.2
        [Test]
        public void TestVectorHMACSHA256_5()
        {
            var key = new NativeArray<byte>(131, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < 131; i++)
                key[i] = 0xaa;

            FixedString512 message = "Test Using Larger Than Block-Size Key - Hash Key First";
            FixedString128 expectedResult = "60e431591ee0b67f0d8a26aacbf5b77f8e0bc6213728c5140546040f0ee37f54";

            Assert.AreEqual(131, key.Length);

            ValidateHMACSHA256TestVector(key.ToArray(), ToArray(message.ToString()), ref expectedResult);
        }

        // https://datatracker.ietf.org/doc/html/rfc4231#section-4.2
        [Test]
        public void TestVectorHMACSHA256_6()
        {
            var key = new NativeArray<byte>(131, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < 131; i++)
                key[i] = 0xaa;

            FixedString512 message = "This is a test using a larger than block-size key and a larger than block-size data. The key needs to be hashed before being used by the HMAC algorithm.";
            FixedString128 expectedResult = "9b09ffa71b942fcb27635fbcd5b0e944bfdc63644f0713938a7f51535c3a35e2";

            Assert.AreEqual(131, key.Length);

            ValidateHMACSHA256TestVector(key.ToArray(), ToArray(message.ToString()), ref expectedResult);
        }

        void AssertAreEqualSHA(IEnumerable<byte> expected, IEnumerable<byte> actual, string message)
        {
            Assert.IsTrue(expected.SequenceEqual(actual), message);
        }

        static byte[] ToArray(string src)
        {
            return Encoding.UTF8.GetBytes(src);
        }

#if !NET_DOTS
        [Test]
        public void TestReferenceImplementation1()
        {
            GenerateAndCompareHMAC(42, 10, 100);
        }

        [Test]
        public void TestReferenceImplementation2()
        {
            GenerateAndCompareHMAC(31242, 10, 0);
        }

        [Test]
        public void TestReferenceImplementation3()
        {
            GenerateAndCompareHMAC(86, 10, 10);
        }

        [Test]
        public void TestReferenceImplementation4()
        {
            GenerateAndCompareHMAC(512, 100, 100);
        }

        [Test]
        public void TestReferenceImplementation5()
        {
            GenerateAndCompareHMAC(51241, 464, 2552);
        }

        [Test]
        public void SHATestReferenceImplementation1()
        {
            GenerateAndCompareSHA(42, 10);
        }

        [Test]
        public void SHATestReferenceImplementation2()
        {
            GenerateAndCompareSHA(242, 0);
        }

        [Test]
        public void SHATestReferenceImplementation3()
        {
            GenerateAndCompareSHA(2422, 2130);
        }

        [Test]
        public void TestReferenceImplementationAll()
        {
            var rnd = new Random(42);

            for (int i = 0; i < 128; i++)
            {
                GenerateAndCompareSHA(rnd.Next(), rnd.Next(4096));
            }

            for (int i = 0; i < 128; i++)
            {
                GenerateAndCompareHMAC(rnd.Next(), rnd.Next(4096), rnd.Next(4096*8));
            }
        }

        private void ValidateWithReferenceImplementationHMAC(byte[] key, byte[] message, IEnumerable<byte> result)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA256(key))
            {
                var resultCorrect = hmac.ComputeHash(message);
                AssertAreEqualSHA(resultCorrect, result, "Cryptography.HMACSHA256 gives different results!");
            }
        }

        private void ValidateWithReferenceImplementationSHA256(byte[] message, IEnumerable<byte> result)
        {
            using (var hmac = new System.Security.Cryptography.SHA256Managed())
            {
                var resultCorrect = hmac.ComputeHash(message);
                AssertAreEqualSHA(resultCorrect, result, "Cryptography.SHA256Managed gives different results!");
            }
        }

        private unsafe void GenerateAndCompareHMAC(int seed, int keyLength, int messageLength)
        {
            var rnd = new Random(seed);
            var key = GenerateSequence(rnd, keyLength);
            var message = GenerateSequence(rnd, messageLength);
            var resultToValidate = new NativeArray<byte>(32, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            HMACSHA256.ComputeHash((byte*) key.GetUnsafeReadOnlyPtr(), key.Length,
                                   (byte*) message.GetUnsafeReadOnlyPtr(), message.Length,
                                   (byte*) resultToValidate.GetUnsafePtr());

            ValidateWithReferenceImplementationHMAC(key.ToArray(), message.ToArray(), resultToValidate);
        }

        private unsafe void GenerateAndCompareSHA(int seed, int messageLength)
        {
            var rnd = new Random(seed);
            var message = GenerateSequence(rnd, messageLength);
            var result = new NativeArray<byte>(32, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            var sha256State = SHA256.SHA256State.Create();
            sha256State.Update((byte*) message.GetUnsafePtr(), message.Length);
            sha256State.Final((byte*) result.GetUnsafePtr());

            ValidateWithReferenceImplementationSHA256(message.ToArray(), result);
        }

        private NativeArray<byte> GenerateSequence(Random rnd, int size)
        {
            var res = new NativeArray<byte>(size, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < size; i++)
            {
                res[i] = (byte)rnd.Next(255);
            }
            return res;
        }
#endif
    }
}