using System;
using System.Collections.Generic;
using Unity.Services.Authentication.Editor.Models;
using Unity.Services.Authentication.Utilities;
using UnityEngine;
using ILogger = Unity.Services.Authentication.Utilities.ILogger;

namespace Unity.Services.Authentication.Editor
{
    interface IAuthenticationAdminNetworkClient
    {
        IWebRequest<TokenExchangeResponse> TokenExchange(string token);
        IWebRequest<GetIdDomainResponse> GetDefaultIdDomain(string token);
        IWebRequest<IdProviderResponse> CreateIdProvider(CreateIdProviderRequest body, string idDomain, string token);
        IWebRequest<ListIdProviderResponse> ListIdProvider(string idDomain, string token);
        IWebRequest<IdProviderResponse> UpdateIdProvider(UpdateIdProviderRequest body, string idDomain, string type, string token);
        IWebRequest<IdProviderResponse> EnableIdProvider(string idDomain, string type, string token);
        IWebRequest<IdProviderResponse> DisableIdProvider(string idDomain, string type, string token);
        IWebRequest<IdProviderResponse> DeleteIdProvider(string idDomain, string type, string token);
    }

    class AuthenticationAdminNetworkClient : IAuthenticationAdminNetworkClient
    {
        const string k_ServicesGatewayStem = "/api/player-identity/v1/organizations/";
        const string k_GetDefaultIdDomainStem = "/iddomains/default";
        const string k_TokenExchangeStem = "/api/auth/v1/genesis-token-exchange/unity";

        readonly string m_ServicesGatewayHost;

        readonly string m_GetDefaultIdDomainUrl;
        readonly string m_TokenExchangeUrl;

        readonly string m_OrganizationId;
        readonly string m_ProjectId;

        readonly INetworkingUtilities m_NetworkClient;

        readonly Dictionary<string, string> m_CommonPlayerIdentityHeaders;

        internal AuthenticationAdminNetworkClient(string servicesGatewayHost,
                                                  string organizationId,
                                                  string projectId,
                                                  INetworkingUtilities networkClient,
                                                  ILogger logger)
        {
            m_ServicesGatewayHost = servicesGatewayHost;
            m_OrganizationId = organizationId;
            m_ProjectId = projectId;

            m_GetDefaultIdDomainUrl = servicesGatewayHost + k_ServicesGatewayStem + organizationId + k_GetDefaultIdDomainStem;
            m_TokenExchangeUrl = servicesGatewayHost + k_TokenExchangeStem;
            m_NetworkClient = networkClient;

            m_CommonPlayerIdentityHeaders = new Dictionary<string, string>
            {
                ["ProjectId"] = projectId,
                // The Error-Version header enables RFC7807HttpError error responses
                ["Error-Version"] = "v1"
            };
        }

        public IWebRequest<GetIdDomainResponse> GetDefaultIdDomain(string token)
        {
            return m_NetworkClient.Get<GetIdDomainResponse>(m_GetDefaultIdDomainUrl, addTokenHeader(m_CommonPlayerIdentityHeaders, token));
        }

        public IWebRequest<TokenExchangeResponse> TokenExchange(string token)
        {
            var body = new TokenExchangeRequest();
            body.Token = token;
            return m_NetworkClient.PostJson<TokenExchangeResponse>(m_TokenExchangeUrl, body);
        }

        public IWebRequest<IdProviderResponse> CreateIdProvider(CreateIdProviderRequest body, string idDomain, string token)
        {
            return m_NetworkClient.PostJson<IdProviderResponse>(CreateIdProviderUrl(idDomain), body, addTokenHeader(m_CommonPlayerIdentityHeaders, token));
        }

        public IWebRequest<ListIdProviderResponse> ListIdProvider(string idDomain, string token)
        {
            return m_NetworkClient.Get<ListIdProviderResponse>(ListIdProviderUrl(idDomain), addTokenHeader(m_CommonPlayerIdentityHeaders, token));
        }

        public IWebRequest<IdProviderResponse> UpdateIdProvider(UpdateIdProviderRequest body, string idDomain, string type, string token)
        {
            return m_NetworkClient.Put<IdProviderResponse>(UpdateIdProviderUrl(idDomain, type), body, addTokenHeader(m_CommonPlayerIdentityHeaders, token));
        }

        public IWebRequest<IdProviderResponse> EnableIdProvider(string idDomain, string type, string token)
        {
            return m_NetworkClient.Post<IdProviderResponse>(EnableIdProviderUrl(idDomain, type), addJsonHeader(addTokenHeader(m_CommonPlayerIdentityHeaders, token)));
        }

        public IWebRequest<IdProviderResponse> DisableIdProvider(string idDomain, string type, string token)
        {
            return m_NetworkClient.Post<IdProviderResponse>(DisableIdProviderUrl(idDomain, type), addJsonHeader(addTokenHeader(m_CommonPlayerIdentityHeaders, token)));
        }

        public IWebRequest<IdProviderResponse> DeleteIdProvider(string idDomain, string type, string token)
        {
            return m_NetworkClient.Delete<IdProviderResponse>(DeleteIdProviderUrl(idDomain, type), addTokenHeader(m_CommonPlayerIdentityHeaders, token));
        }

        Dictionary<string, string> addTokenHeader(Dictionary<string, string> d, string token)
        {
            var headers = new Dictionary<string, string>(d);
            headers.Add("Authorization", "Bearer "  + token);
            return headers;
        }

        Dictionary<string, string> addJsonHeader(Dictionary<string, string> d)
        {
            var headers = new Dictionary<string, string>(d);
            headers.Add("Content-Type", "application/json");
            return headers;
        }

        string CreateIdProviderUrl(string idDomain)
        {
            return m_ServicesGatewayHost + k_ServicesGatewayStem + m_OrganizationId + "/iddomains/" + idDomain + "/idps";
        }

        string ListIdProviderUrl(string idDomain)
        {
            return m_ServicesGatewayHost + k_ServicesGatewayStem + m_OrganizationId + "/iddomains/" + idDomain + "/idps";
        }

        string UpdateIdProviderUrl(string idDomain, string type)
        {
            return m_ServicesGatewayHost + k_ServicesGatewayStem + m_OrganizationId + "/iddomains/" + idDomain + "/idps/" + type;
        }

        string DeleteIdProviderUrl(string idDomain, string type)
        {
            return m_ServicesGatewayHost + k_ServicesGatewayStem + m_OrganizationId + "/iddomains/" + idDomain + "/idps/" + type;
        }

        string EnableIdProviderUrl(string idDomain, string type)
        {
            return m_ServicesGatewayHost + k_ServicesGatewayStem + m_OrganizationId + "/iddomains/" + idDomain + "/idps/" + type + "/enable";
        }

        string DisableIdProviderUrl(string idDomain, string type)
        {
            return m_ServicesGatewayHost + k_ServicesGatewayStem + m_OrganizationId + "/iddomains/" + idDomain + "/idps/" + type + "/disable";
        }
    }
}
