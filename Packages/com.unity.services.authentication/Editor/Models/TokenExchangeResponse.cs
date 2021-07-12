using System;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Unity.Services.Authentication.Editor.Models
{
    [Serializable]
    class TokenExchangeResponse
    {
        [Preserve]
        public TokenExchangeResponse() {}

        [JsonProperty("token")]
        public string Token;
    }
}
