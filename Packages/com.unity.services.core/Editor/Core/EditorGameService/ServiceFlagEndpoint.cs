using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Unity.Services.Core.Editor
{
    class ServiceFlagEndpoint : CdnConfiguredEndpoint<ServiceFlagEndpointConfiguration> {}

    class ServiceFlagEndpointConfiguration
    {
        const string k_ServiceFlagFormat = "/projects/{0}/service_flags";

        [JsonProperty("core")]
        public string Core { get; set; }

        string BuildApiUrl()
        {
            return Core + "/api";
        }

        public string BuildServiceFlagUrl(string projectId)
        {
            return string.Format(BuildApiUrl() + k_ServiceFlagFormat, projectId);
        }

        public string BuildPayload(string serviceFlagName, bool status)
        {
            return JsonConvert.SerializeObject(new ServiceFlagPayload(serviceFlagName, status));
        }

        class ServiceFlagPayload
        {
            // A Dictionary is used here because both the key and the value must be mutable
            // the key is the the service flag name and the value is the status bool
            [JsonProperty("service_flags")]
            Dictionary<string, bool> m_ServiceFlag;

            public ServiceFlagPayload(string serviceFlagName, bool status)
            {
                m_ServiceFlag = new Dictionary<string, bool> { { serviceFlagName, status } };
            }
        }
    }
}
