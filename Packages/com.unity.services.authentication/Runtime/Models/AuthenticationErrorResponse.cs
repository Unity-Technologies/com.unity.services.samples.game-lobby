using System;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Unity.Services.Authentication.Models
{
    /// <summary>
    /// The model for error response from authentication server.
    /// </summary>
    /// <remarks>
    /// There is another field "details" in the error response. It provides additional details
    /// to the error. It's ignored in this deserialized class since it's not needed by the client SDK.
    /// </remarks>
    [Serializable]
    class AuthenticationErrorResponse
    {
        [Preserve]
        public AuthenticationErrorResponse() {}

        [JsonProperty("title")]
        public string Title;

        [JsonProperty("detail")]
        public string Detail;

        [JsonProperty("status")]
        public int Status;
    }
}
