using System;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Unity.Services.Authentication.Models
{
    [Serializable]
    class ExternalTokenRequest
    {
        [Preserve]
        public ExternalTokenRequest() {}

        [JsonProperty("idProvider")]
        public string IdProvider;

        [JsonProperty("token")]
        public string Token;
    }
}
