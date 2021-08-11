using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Unity.Services.Authentication.Models;
using Unity.Services.Authentication.Utilities;
using Unity.Services.Core.Environments.Internal;
using Logger = Unity.Services.Authentication.Utilities.Logger;

namespace Unity.Services.Authentication
{
    interface IAuthenticationNetworkClient
    {
        IWebRequest<WellKnownKeys> GetWellKnownKeys();
        IWebRequest<SignInResponse> SignInAnonymously();
        IWebRequest<SignInResponse> SignInWithSessionToken(string token);
        IWebRequest<SignInResponse> SignInWithExternalToken(ExternalTokenRequest externalToken);
        IWebRequest<SignInResponse> LinkWithExternalToken(string accessToken, ExternalTokenRequest externalToken);
    }

    class AuthenticationNetworkClient : IAuthenticationNetworkClient
    {
        const string k_WellKnownUrlStem = "/.well-known/jwks.json";
        const string k_AnonymousUrlStem = "/authentication/anonymous";
        const string k_SessionTokenUrlStem = "/authentication/session-token";
        const string k_ExternalTokenUrlStem = "/authentication/external-token";
        const string k_LinkExternalTokenUrlStem = "/authentication/link";
        const string k_OAuthUrlStem = "/oauth2/auth";
        const string k_OAuthTokenUrlStem = "/oauth2/token";
        const string k_OAuthScope = "openid offline unity.user identity.user";
        const string k_AuthResponseType = "code";
        const string k_ChallengeMethod = "S256";
        const string k_OauthRevokeStem = "/oauth2/revoke";

        readonly INetworkingUtilities m_NetworkClient;
        readonly ICodeChallengeGenerator m_CodeChallengeGenerator;

        readonly string m_WellKnownUrl;
        readonly string m_AnonymousUrl;
        readonly string m_SessionTokenUrl;
        readonly string m_ExternalTokenUrl;
        readonly string m_LinkExternalTokenUrl;
        readonly string m_OAuthUrl;
        readonly string m_OAuthTokenUrl;
        readonly string m_OAuthRevokeTokenUrl;

        readonly Dictionary<string, string> m_CommonHeaders;

        string m_OAuthClientId;
        string m_SessionChallengeCode;

        /// <summary>
        /// the environments component in the core registry.
        /// this is stored in case there is a reinitialization or a change in environments ever happens during runtime.
        /// </summary>
        IEnvironments m_EnvironmentComponent;

        internal AuthenticationNetworkClient(string authenticationHost,
                                             string projectId,
                                             IEnvironments environment,
                                             ICodeChallengeGenerator codeChallengeGenerator,
                                             INetworkingUtilities networkClient)
        {
            m_NetworkClient = networkClient;
            m_CodeChallengeGenerator = codeChallengeGenerator;

            m_OAuthClientId = "default";

            m_WellKnownUrl = authenticationHost + k_WellKnownUrlStem;
            m_AnonymousUrl = authenticationHost + k_AnonymousUrlStem;
            m_SessionTokenUrl = authenticationHost + k_SessionTokenUrlStem;
            m_ExternalTokenUrl = authenticationHost + k_ExternalTokenUrlStem;
            m_LinkExternalTokenUrl = authenticationHost + k_LinkExternalTokenUrlStem;
            m_OAuthUrl = authenticationHost + k_OAuthUrlStem;
            m_OAuthTokenUrl = authenticationHost + k_OAuthTokenUrlStem;
            m_OAuthRevokeTokenUrl = authenticationHost + k_OauthRevokeStem;

            m_EnvironmentComponent = environment;

            m_CommonHeaders = new Dictionary<string, string>
            {
                ["ProjectId"] = projectId,
                // The Error-Version header enables RFC7807HttpError error responses
                ["Error-Version"] = "v1"
            };
        }

        public IWebRequest<WellKnownKeys> GetWellKnownKeys()
        {
            return m_NetworkClient.Get<WellKnownKeys>(m_WellKnownUrl);
        }

        public void SetOAuthClient(string oAuthClientId)
        {
            m_OAuthClientId = oAuthClientId;
        }

        public IWebRequest<SignInResponse> SignInAnonymously()
        {
            return m_NetworkClient.Post<SignInResponse>(m_AnonymousUrl, WithEnvironment(GetCommonHeaders()));
        }

        public IWebRequest<SignInResponse> SignInWithSessionToken(string token)
        {
            return m_NetworkClient.PostJson<SignInResponse>(m_SessionTokenUrl, new SessionTokenRequest
            {
                SessionToken = token
            }, WithEnvironment(GetCommonHeaders()));
        }

        public IWebRequest<SignInResponse> SignInWithExternalToken(ExternalTokenRequest externalToken)
        {
            return m_NetworkClient.PostJson<SignInResponse>(m_ExternalTokenUrl, externalToken, WithEnvironment(GetCommonHeaders()));
        }

        public IWebRequest<SignInResponse> LinkWithExternalToken(string accessToken, ExternalTokenRequest externalToken)
        {
            return m_NetworkClient.PostJson<SignInResponse>(m_LinkExternalTokenUrl, externalToken, WithEnvironment(WithAccessToken(GetCommonHeaders(), accessToken)));
        }

        public IWebRequest<OAuthAuthCodeResponse> RequestAuthCode(string idToken)
        {
            m_SessionChallengeCode = m_CodeChallengeGenerator.GenerateCode();

            var payload = $"client_id={m_OAuthClientId}&" +
                $"response_type={k_AuthResponseType}&" +
                $"id_token={idToken}&" +
                $"state={m_CodeChallengeGenerator.GenerateStateString()}&" +
                $"scope={k_OAuthScope}&" +
                $"code_challenge={S256EncodeChallenge(m_SessionChallengeCode)}&" +
                $"code_challenge_method={k_ChallengeMethod}";

            return m_NetworkClient.PostForm<OAuthAuthCodeResponse>(m_OAuthUrl, payload, m_CommonHeaders);
        }

        string S256EncodeChallenge(string code)
        {
            using (var sha256 = SHA256.Create())
            {
                var codeVerifierBytes = Encoding.UTF8.GetBytes(code);
                var codeVerifierHash = sha256.ComputeHash(codeVerifierBytes);
                return UrlSafeBase64Encode(codeVerifierHash);
            }
        }

        string UrlSafeBase64Encode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");
        }

        public string ExtractAuthCode(IWebRequest<OAuthAuthCodeResponse> authCodeRequest)
        {
            try
            {
                var locationUri = new Uri(authCodeRequest.ResponseHeaders["Location"]);
                return ExtractAuthCode(locationUri.ToString(), locationUri.Query);
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to extract auth code. " + ex.Message);
                Logger.LogException(ex);
                return null;
            }
        }

        string ExtractAuthCode(string locationUri, string query)
        {
            var queryParams = HttpUtilities.ParseQueryString(query);

            string code;
            if (!queryParams.TryGetValue("code", out code))
            {
                Logger.LogError($"Failed to extract auth code. Query parameter 'code' is not found. Location: {locationUri}");
            }

            return code;
        }

        public IWebRequest<OAuthTokenResponse> RequestOAuthToken(string authCode)
        {
            var payload = $"client_id={m_OAuthClientId}" +
                "&grant_type=authorization_code" +
                $"&code_verifier={m_SessionChallengeCode}" +
                $"&code={authCode}";

            return m_NetworkClient.PostForm<OAuthTokenResponse>(m_OAuthTokenUrl, payload, m_CommonHeaders);
        }

        public IWebRequest<OAuthTokenResponse> RefreshOAuthToken(string refreshToken)
        {
            var payload = "grant_type=refresh_token" +
                $"&client_id={m_OAuthClientId}" +
                $"&refresh_token={refreshToken}";

            return m_NetworkClient.PostForm<OAuthTokenResponse>(m_OAuthTokenUrl, payload, m_CommonHeaders, 5);
        }

        public IWebRequest<OAuthTokenResponse> RevokeOAuthToken(string accessToken)
        {
            var payload = $"client_id={m_OAuthClientId}&token={accessToken}";

            return m_NetworkClient.PostForm<OAuthTokenResponse>(m_OAuthRevokeTokenUrl, payload, m_CommonHeaders);
        }

        public string ExtractAccessToken(IWebRequest<OAuthTokenResponse> authCodeRequest)
        {
            return authCodeRequest.ResponseBody.AccessToken;
        }

        Dictionary<string, string> WithAccessToken(Dictionary<string, string> headers, string accessToken)
        {
            headers["Authorization"] = "Bearer " + accessToken;
            return headers;
        }

        Dictionary<string, string> WithEnvironment(Dictionary<string, string> headers)
        {
            var env = m_EnvironmentComponent.Current;
            if (!string.IsNullOrEmpty(env))
            {
                headers["UnityEnvironment"] = m_EnvironmentComponent.Current;
            }
            return headers;
        }

        Dictionary<string, string> GetCommonHeaders()
        {
            return new Dictionary<string, string>(m_CommonHeaders);
        }
    }
}
