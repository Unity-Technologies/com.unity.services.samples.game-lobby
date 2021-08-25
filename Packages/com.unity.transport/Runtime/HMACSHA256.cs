using Unity.Collections;

namespace Unity.Networking.Transport
{
    public static class HMACSHA256
    {
        /// <summary>
        /// Writes 32 bytes to result using key and message
        /// </summary>
        /// <param name="keyValue">Key data</param>
        /// <param name="keyArrayLength">Length of the key data</param>
        /// <param name="messageBytes">Message to hash</param>
        /// <param name="messageLength">Length of the message</param>
        /// <param name="result">Where to write resulting 32 bytes hash</param>
        public static unsafe void ComputeHash(byte* keyValue, int keyArrayLength, byte* messageBytes, int messageLength, byte* result)
        {
            const int B = 64;
            const int sha256SizeBytes = 32;
            const byte ipad = 0x36;
            const byte opad = 0x5C;

            var shorterKey = stackalloc byte[sha256SizeBytes];

            var sha256State = SHA256.SHA256State.Create();

            if (keyArrayLength > B)
            {
                sha256State.Update(keyValue, keyArrayLength);
                sha256State.Final(shorterKey);

                keyValue = shorterKey;
                keyArrayLength = sha256SizeBytes;
            }

            var kx = stackalloc byte[B];
            for (var i = 0; i < keyArrayLength; i++)
                kx[i] = (byte) (ipad ^ keyValue[i]);
            for (var i = keyArrayLength; i < B; i++)
                kx[i] = ipad;

            sha256State = SHA256.SHA256State.Create();
            sha256State.Update(kx, B);
            sha256State.Update(messageBytes, messageLength);
            sha256State.Final(result);

            for (var i = 0; i < keyArrayLength; i++)
                kx[i] = (byte) (opad ^ keyValue[i]);
            for (var i = keyArrayLength; i < B; i++)
                kx[i] = opad;

            sha256State = SHA256.SHA256State.Create();
            sha256State.Update(kx, B);
            sha256State.Update(result, sha256SizeBytes);
            sha256State.Final(result);
        }
    }
}