using System;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Unity.Services.Authentication.Editor.Models
{
    [Serializable]
    class TokenExchangeRequest
    {
        [Preserve]
        public TokenExchangeRequest() {}

        [JsonProperty("token")]
        public string Token;
    }
}
