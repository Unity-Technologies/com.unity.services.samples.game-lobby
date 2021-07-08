using System;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Unity.Services.Authentication.Editor.Models
{
    [Serializable]
    class DeleteIdProviderRequest
    {
        [Preserve]
        public DeleteIdProviderRequest() {}

        [JsonProperty("IdDomain")]
        public string IdDomain;        

        // string type
        [JsonProperty("type")]
        public string Type;
    }
}
