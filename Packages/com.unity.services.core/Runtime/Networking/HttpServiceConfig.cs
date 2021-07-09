using System;

namespace Unity.Services.Core.Networking
{
    [Serializable]
    struct HttpServiceConfig
    {
        public string ServiceId;

        public string BaseUrl;

        public HttpOptions DefaultOptions;
    }
}
