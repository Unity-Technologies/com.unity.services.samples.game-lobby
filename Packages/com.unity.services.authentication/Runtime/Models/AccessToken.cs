using System;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Unity.Services.Authentication.Models
{
    class AccessToken : BaseJwt
    {
        [Preserve]
        public AccessToken() {}

        [JsonProperty("aud")]
        public string[] Audience;

        [JsonProperty("client_id")]
        public string ClientId;

        [JsonProperty("ext")]
        public AccessTokenExtraClaims Extra;

        [JsonProperty("iss")]
        public string Issuer;

        [JsonProperty("jti")]
        public string JwtId;

        [JsonProperty("project_id")]
        public string ProjectId;

        [JsonProperty("scp")]
        public string[] Scope;

        [JsonProperty("sub")]
        public string Subject;
    }

    class AccessTokenExtraClaims
    {
        [Preserve]
        public AccessTokenExtraClaims() {}

        // Unused at this time but left in for completeness
    }
}
