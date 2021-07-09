using System;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Unity.Services.Authentication.Editor.Models
{
    [Serializable]
    class ListIdProviderResponse
    {
        [Preserve]
        public ListIdProviderResponse() {}

        [JsonProperty("total")]
        public int Total;

        [JsonProperty("results")]
        public IdProviderResponse[] Results;
    }
}
