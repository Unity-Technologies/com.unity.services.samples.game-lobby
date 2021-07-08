using System;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Unity.Services.Authentication.Models
{
    [Serializable]
    class OAuthTokenResponse
    {
        [Preserve]
        public OAuthTokenResponse() {}

        [JsonProperty("access_token")]
        public string AccessToken;

        [JsonProperty("id_token")]
        public string IdToken;

        [JsonProperty("refresh_token")]
        public string RefreshToken;

        [JsonProperty("scope")]
        public string Scope;

        [JsonProperty("token_type")]
        public string TokenType;

        [JsonProperty("expires_in")]
        public long ExpiresIn;
    }
}
