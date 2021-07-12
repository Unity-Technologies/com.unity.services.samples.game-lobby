using System;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Unity.Services.Authentication.Editor.Models
{
    /// <summary>
    /// The model for error response from authentication server.
    /// </summary>
    /// <remarks>
    /// There is another field "details" in the error response. It provides additional details
    /// to the error. It's ignored in this deserialized class since it's not needed by the client SDK.
    /// </remarks>
    [Serializable]
    class TokenExchangeErrorResponse
    {
        [Preserve]
        public TokenExchangeErrorResponse() {}

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("message")]
        public string Message;

        [JsonProperty("status")]
        public int Status;
    }
}
