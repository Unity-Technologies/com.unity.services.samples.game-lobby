using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Unity.Services.Authentication.Editor.Models;
using Unity.Services.Authentication.Models;
using Unity.Services.Authentication.Utilities;
using Unity.Services.Core.Internal;
using UnityEditor;
using Logger = Unity.Services.Authentication.Utilities.Logger;

[assembly: InternalsVisibleTo("Unity.Services.Authentication.Editor.Tests")]
[assembly: InternalsVisibleTo("Unity.Services.Authentication.EditorTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // For Moq

namespace Unity.Services.Authentication.Editor
{
    static class AuthenticationAdminClientManager
    {
        internal static IAuthenticationAdminClient Instance { get; set; } = AuthenticationAdminClient();

        static IAuthenticationAdminClient AuthenticationAdminClient()
        {
            IDateTimeWrapper dateTime = new DateTimeWrapper();
            INetworkingUtilities networkUtilities = new NetworkingUtilities(null);
            string orgId = GetOrganizationId();
            var networkClient = new AuthenticationAdminNetworkClient("https://services.unity.com",
                orgId,
                CloudProjectSettings.projectId,
                networkUtilities);

            return new AuthenticationAdminClient(networkClient);
        }

        // GetOrganizationId will gets the organization id associated with this Unity project.
        static string GetOrganizationId()
        {
            // This is a temporary workaround to get the Genesis organization foreign key for non-DevX enhanced Unity versions.
            // When the eventual changes are backported into previous versions of Unity, this will no longer be necessary.
            Assembly assembly = Assembly.GetAssembly(typeof(EditorWindow));
            var unityConnectInstance = assembly.CreateInstance("UnityEditor.Connect.UnityConnect", false, BindingFlags.NonPublic | BindingFlags.Instance, null, null, null, null); Type t = unityConnectInstance.GetType();
            var projectInfo = t.GetProperty("projectInfo").GetValue(unityConnectInstance, null);

            Type projectInfoType = projectInfo.GetType();
            return projectInfoType.GetProperty("organizationForeignKey").GetValue(projectInfo, null) as string;
        }
    }

    class AuthenticationAdminClient : IAuthenticationAdminClient
    {
        string m_IdDomain;
        IAuthenticationAdminNetworkClient m_AuthenticationAdminNetworkClient;

        string m_orgForeignKey;
        string m_servicesGatewayToken;
        string m_genesisToken;

        internal enum ServiceCalled
        {
            TokenExchange,
            AuthenticationAdmin
        }

        public AuthenticationAdminClient(IAuthenticationAdminNetworkClient networkClient, string genesisToken = "")
        {
            m_AuthenticationAdminNetworkClient = networkClient;
            m_genesisToken = genesisToken;
        }

        public IAsyncOperation<string> GetIDDomain()
        {
            var asyncOp = new AsyncOperation<string>();
            Action<string> getIdDomainFunc = token =>
            {
                var getDefaultIdDomainRequest = m_AuthenticationAdminNetworkClient.GetDefaultIdDomain(token);
                getDefaultIdDomainRequest.Completed += request => HandleGetIdDomainAPICall(asyncOp, request);
            };

            getGenesisToken();
            var tokenAsyncOp = ExchangeToken(m_genesisToken);
            tokenAsyncOp.Completed += tokenAsyncOpResult => getIdDomainFunc(tokenAsyncOpResult?.Result);
            return asyncOp;
        }

        public IAsyncOperation<IdProviderResponse> CreateIdProvider(string iddomain, CreateIdProviderRequest body)
        {
            var asyncOp = new AsyncOperation<IdProviderResponse>();
            Action<string> createIdProviderFunc = token =>
            {
                var request = m_AuthenticationAdminNetworkClient.CreateIdProvider(body, iddomain, token);
                request.Completed += req => HandleIdProviderResponseApiCall(asyncOp, req);
            };

            getGenesisToken();
            var tokenAsyncOp = ExchangeToken(m_genesisToken);
            tokenAsyncOp.Completed += tokenAsyncOpResult => createIdProviderFunc(tokenAsyncOpResult?.Result);
            return asyncOp;
        }

        public IAsyncOperation<ListIdProviderResponse> ListIdProviders(string iddomain)
        {
            var asyncOp = new AsyncOperation<ListIdProviderResponse>();
            Action<string> listIdProviderFunc = token =>
            {
                var request = m_AuthenticationAdminNetworkClient.ListIdProvider(iddomain, token);
                request.Completed += req => HandleListIdProviderResponseApiCall(asyncOp, req);
            };

            getGenesisToken();
            var tokenAsyncOp = ExchangeToken(m_genesisToken);
            tokenAsyncOp.Completed += tokenAsyncOpResult => listIdProviderFunc(tokenAsyncOpResult?.Result);
            return asyncOp;
        }

        public IAsyncOperation<IdProviderResponse> UpdateIdProvider(string iddomain, string type, UpdateIdProviderRequest body)
        {
            var asyncOp = new AsyncOperation<IdProviderResponse>();
            Action<string> enableIdProviderFunc = token =>
            {
                var request = m_AuthenticationAdminNetworkClient.UpdateIdProvider(body, iddomain, type, token);
                request.Completed += req => HandleIdProviderResponseApiCall(asyncOp, req);
            };

            getGenesisToken();
            var tokenAsyncOp = ExchangeToken(m_genesisToken);
            tokenAsyncOp.Completed += tokenAsyncOpResult => enableIdProviderFunc(tokenAsyncOpResult?.Result);
            return asyncOp;
        }

        public IAsyncOperation<IdProviderResponse> EnableIdProvider(string iddomain, string type)
        {
            var asyncOp = new AsyncOperation<IdProviderResponse>();
            Action<string> enableIdProviderFunc = token =>
            {
                var request = m_AuthenticationAdminNetworkClient.EnableIdProvider(iddomain, type, token);
                request.Completed += req => HandleIdProviderResponseApiCall(asyncOp, req);
            };

            getGenesisToken();
            var tokenAsyncOp = ExchangeToken(m_genesisToken);
            tokenAsyncOp.Completed += tokenAsyncOpResult => enableIdProviderFunc(tokenAsyncOpResult?.Result);
            return asyncOp;
        }

        public IAsyncOperation<IdProviderResponse> DisableIdProvider(string iddomain, string type)
        {
            var asyncOp = new AsyncOperation<IdProviderResponse>();
            Action<string> disableIdProviderFunc = token =>
            {
                var request = m_AuthenticationAdminNetworkClient.DisableIdProvider(iddomain, type, token);
                request.Completed += req => HandleIdProviderResponseApiCall(asyncOp, req);
            };

            getGenesisToken();
            var tokenAsyncOp = ExchangeToken(m_genesisToken);
            tokenAsyncOp.Completed += tokenAsyncOpResult => disableIdProviderFunc(tokenAsyncOpResult?.Result);
            return asyncOp;
        }

        public IAsyncOperation<IdProviderResponse> DeleteIdProvider(string iddomain, string type)
        {
            var asyncOp = new AsyncOperation<IdProviderResponse>();
            Action<string> deleteIdProviderFunc = token =>
            {
                var request = m_AuthenticationAdminNetworkClient.DeleteIdProvider(iddomain, type, token);
                request.Completed += req => HandleIdProviderResponseApiCall(asyncOp, req);
            };

            getGenesisToken();
            var tokenAsyncOp = ExchangeToken(m_genesisToken);
            tokenAsyncOp.Completed += tokenAsyncOpResult => deleteIdProviderFunc(tokenAsyncOpResult?.Result);
            return asyncOp;
        }

        public IdProviderResponse CloneIdProvider(IdProviderResponse x)
        {
            return x.Clone();
        }

        internal IAsyncOperation<string> ExchangeToken(string token)
        {
            var asyncOp = new AsyncOperation<string>();
            var request = m_AuthenticationAdminNetworkClient.TokenExchange(token);
            request.Completed += req => HandleTokenExchange(asyncOp, req);
            return asyncOp;
        }

        void HandleGetIdDomainAPICall(AsyncOperation<string> asyncOp, IWebRequest<GetIdDomainResponse> request)
        {
            if (HandleError(asyncOp, request, ServiceCalled.AuthenticationAdmin))
            {
                return;
            }

            m_IdDomain = request?.ResponseBody?.Id;
            asyncOp.Succeed(request?.ResponseBody?.Id);
        }

        void HandleTokenExchange(AsyncOperation<string> asyncOp, IWebRequest<TokenExchangeResponse> request)
        {
            if (HandleError(asyncOp, request, ServiceCalled.TokenExchange))
            {
                return;
            }

            var token = request?.ResponseBody?.Token;
            m_servicesGatewayToken = token;
            asyncOp.Succeed(token);
        }

        void HandleIdProviderResponseApiCall(AsyncOperation<IdProviderResponse> asyncOp, IWebRequest<IdProviderResponse> request)
        {
            if (HandleError(asyncOp, request, ServiceCalled.AuthenticationAdmin))
            {
                return;
            }

            asyncOp.Succeed(request?.ResponseBody);
        }

        void HandleListIdProviderResponseApiCall(AsyncOperation<ListIdProviderResponse> asyncOp, IWebRequest<ListIdProviderResponse> request)
        {
            if (HandleError(asyncOp, request, ServiceCalled.AuthenticationAdmin))
            {
                return;
            }

            asyncOp.Succeed(request?.ResponseBody);
        }

        void HandleEmptyResponseApiCall(AsyncOperation<IdProviderResponse> asyncOp, IWebRequest<IdProviderResponse> request)
        {
            if (HandleError(asyncOp, request, ServiceCalled.AuthenticationAdmin))
            {
                return;
            }

            asyncOp.Succeed(request?.ResponseBody);
        }

        internal bool HandleError<Q, T>(AsyncOperation<Q> asyncOp, IWebRequest<T> request, ServiceCalled sc)
        {
            if (!request.RequestFailed)
            {
                return false;
            }

            if (request.NetworkError)
            {
                asyncOp.Fail(new AuthenticationException(AuthenticationError.NetworkError));
                return true;
            }
            Logger.LogError("Error message: " + request.ErrorMessage);

            try
            {
                switch (sc)
                {
                    case ServiceCalled.TokenExchange:
                        var tokenExchangeErrorResponse = JsonConvert.DeserializeObject<TokenExchangeErrorResponse>(request.ErrorMessage);
                        asyncOp.Fail(new AuthenticationException(tokenExchangeErrorResponse.Name, tokenExchangeErrorResponse.Message));
                        break;
                    case ServiceCalled.AuthenticationAdmin:
                        var authenticationAdminErrorResponse = JsonConvert.DeserializeObject<AuthenticationErrorResponse>(request.ErrorMessage);
                        asyncOp.Fail(new AuthenticationException(authenticationAdminErrorResponse.Title, authenticationAdminErrorResponse.Detail));
                        break;
                    default:
                        asyncOp.Fail(new AuthenticationException(AuthenticationError.UnknownError, "Unknown error"));
                        break;
                }
            }
            catch (JsonException ex)
            {
                Logger.LogException(ex);
                asyncOp.Fail(new AuthenticationException(AuthenticationError.UnknownError, "Failed to deserialize server response: " + request.ErrorMessage));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                asyncOp.Fail(new AuthenticationException(AuthenticationError.UnknownError, "Unknown error deserializing server response: " + request.ErrorMessage));
            }

            return true;
        }

        void getGenesisToken()
        {
            if (m_genesisToken == "")
            {
                m_genesisToken = CloudProjectSettings.accessToken;
            }
        }
    }
}
