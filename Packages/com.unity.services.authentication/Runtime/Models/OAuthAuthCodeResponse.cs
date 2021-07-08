using System;
using UnityEngine.Scripting;

namespace Unity.Services.Authentication.Models
{
    [Serializable]
    class OAuthAuthCodeResponse
    {
        [Preserve]
        public OAuthAuthCodeResponse() {}

        // Note, the response here is empty as there is no content in the response we need -
        // the auth code is in the location header of the response.
    }
}
