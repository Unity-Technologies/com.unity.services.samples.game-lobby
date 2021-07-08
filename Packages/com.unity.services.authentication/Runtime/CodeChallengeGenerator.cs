using System;
using System.Security.Cryptography;
using System.Text;

namespace Unity.Services.Authentication
{
    interface ICodeChallengeGenerator
    {
        string GenerateCode();
        string GenerateStateString();
    }

    class CodeChallengeGenerator : ICodeChallengeGenerator
    {
        internal const int k_CodeLength = 128;

        const string k_CodeChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        readonly StringBuilder m_CodeBuilder;

        internal CodeChallengeGenerator()
        {
            m_CodeBuilder = new StringBuilder(k_CodeLength);
        }

        public string GenerateCode()
        {
            var randomBytes = new byte[k_CodeLength];
            using (var randomNumberGenerator = new RNGCryptoServiceProvider())
            {
                randomNumberGenerator.GetBytes(randomBytes);
            }

            m_CodeBuilder.Clear();
            for (var i = 0; i < k_CodeLength; i++)
            {
                m_CodeBuilder.Append(k_CodeChars[randomBytes[i] % k_CodeChars.Length]);
            }

            return m_CodeBuilder.ToString();
        }

        public string GenerateStateString()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
