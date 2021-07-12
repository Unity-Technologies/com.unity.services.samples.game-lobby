using System;

namespace Unity.Services.Authentication
{
    class AccessTokenComponent : IAccessToken
    {
        IAuthenticationService m_AuthenticationService;

        public AccessTokenComponent(IAuthenticationService service)
        {
            m_AuthenticationService = service;
        }

        public string AccessToken => m_AuthenticationService.AccessToken;
    }
}
