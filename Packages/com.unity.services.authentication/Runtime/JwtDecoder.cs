using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Unity.Services.Authentication.Models;
using Unity.Services.Authentication.Utilities;

namespace Unity.Services.Authentication
{
    interface IJwtDecoder
    {
        T Decode<T>(string token, WellKnownKeys keys) where T : BaseJwt;
    }

    /// <summary>
    /// Trimmed-down and specialized version of:
    /// https://github.com/monry/JWT-for-Unity/blob/master/JWT/JWT.cs
    /// At time of writing, this source was public domain (Creative Commons 0)
    /// </summary>
    class JwtDecoder : IJwtDecoder
    {
        static readonly DateTime k_UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        static readonly char[] k_JwtSeparator = { '.' };

        readonly IDateTimeWrapper m_DateTime;
        readonly ILogger m_Logger;

        internal JwtDecoder(IDateTimeWrapper dateTime, ILogger logger)
        {
            m_DateTime = dateTime;
            m_Logger = logger;
        }

        public T Decode<T>(string token, WellKnownKeys keys) where T : BaseJwt
        {
            var parts = token.Split(k_JwtSeparator);
            if (parts.Length == 3)
            {
                var header = parts[0];
                var payload = parts[1];
                var signature = Base64UrlDecode(parts[2]);

                var headerJson = Encoding.UTF8.GetString(Base64UrlDecode(header));
                var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(payload));

                var headerData = JsonConvert.DeserializeObject<Dictionary<string, string>>(headerJson);
                var payloadData = JsonConvert.DeserializeObject<T>(payloadJson);

                // verify exp claim https://tools.ietf.org/html/draft-ietf-oauth-json-web-token-32#section-4.1.4
                var secondsSinceEpoch = m_DateTime.SecondsSinceUnixEpoch();
                if (secondsSinceEpoch >= payloadData.ExpirationTimeUnix)
                {
                    m_Logger.Error("Token has expired.");
                    return null;
                }

                // NOTE: VerifySignature includes creating a load of cryptography objects, so
                // do it last in case we don't need to.

                if (VerifySignature(header, headerData["kid"], payload, keys, signature))
                {
                    return payloadData;
                }

                m_Logger.Error("Token signature could not be verified.");
                return null;
            }

            m_Logger.Error($"That is not a valid token (expected 3 parts but has {parts.Length}).");
            return null;
        }

        bool VerifySignature(string header, string keyId, string payload, WellKnownKeys keys, byte[] signature)
        {
            var key = GetKey(keyId, keys);

            if (key != null)
            {
                var verified = Verify(header, payload, signature, Base64UrlDecode(key.N), Base64UrlDecode(key.E));
                if (!verified)
                {
                    m_Logger.Error("Signature failed verification!");
                }

                return verified;
            }

            m_Logger.Error("Unable to verify signature, does not use a well-known key ID.");
            return false;
        }

        WellKnownKey GetKey(string keyId, WellKnownKeys keys)
        {
            foreach (var key in keys.Keys)
            {
                if (key.KeyId == keyId)
                {
                    return key;
                }
            }

            return null;
        }

        bool Verify(string header, string payload, byte[] signature, byte[] modulus, byte[] exponent)
        {
            // Based on:
            // https://stackoverflow.com/questions/34403823/verifying-jwt-signed-with-the-rs256-algorithm-using-public-key-in-c-sharp
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(new RSAParameters
                {
                    Modulus = modulus,
                    Exponent = exponent
                });

                using (var sha256 = SHA256.Create())
                {
                    var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(header + '.' + payload));

                    var rsaDeformatter = new RSAPKCS1SignatureDeformatter(rsa);
                    rsaDeformatter.SetHashAlgorithm("SHA256");
                    return rsaDeformatter.VerifySignature(hash, signature);
                }
            }
        }

        byte[] Base64UrlDecode(string input)
        {
            var output = input;
            output = output.Replace('-', '+'); // 62nd char of encoding
            output = output.Replace('_', '/'); // 63rd char of encoding

            var mod4 = input.Length % 4;
            if (mod4 > 0)
            {
                output += new string('=', 4 - mod4);
            }

            var converted = Convert.FromBase64String(output); // Standard base64 decoder
            return converted;
        }
    }
}
