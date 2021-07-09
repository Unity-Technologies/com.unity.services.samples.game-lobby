using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.Authentication.Models;
using Unity.Services.Authentication.Utilities;
using Unity.Services.Core;

[assembly: InternalsVisibleTo("Unity.Services.Authentication.Tests")]
[assembly: InternalsVisibleTo("Unity.Services.Authentication.EditorTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // For Moq

namespace Unity.Services.Authentication
{
    enum AuthenticationState
    {
        SignedOut,

        SigningIn,

        VerifyingAccessToken,

        Authorized,
        Refreshing,
        Expired
    }

    class AuthenticationServiceInternal : IAuthenticationService
    {
        const string k_CacheKeySessionToken = "session_token";

        const string k_IdProviderApple = "apple.com";
        const string k_IdProviderGoogle = "google.com";
        const string k_IdProviderFacebook = "facebook.com";
        const string k_IdProviderSteam = "steampowered.com";

        // NOTE: the REFRESH buffer should always have a larger value than the EXPIRY buffer,
        // i.e. it happens much earlier than the expiry time. The difference should be large
        // enough that the refresh process can be attempted (and retried) and succeed (or not)
        // before the token is actually considered expired.

        /// <summary>
        /// The buffer time in seconds to start access token refresh before the access token expires.
        /// </summary>
        const int k_AccessTokenRefreshBuffer = 120;

        /// <summary>
        /// The buffer time in seconds to treat token as expired before the token's expiry time.
        /// This is to deal with the time difference between the client and server.
        /// </summary>
        const int k_AccessTokenExpiryBuffer = 20;

        /// <summary>
        /// The time in seconds between access token refresh retries.
        /// </summary>
        const int k_ExpiredRefreshAttemptFrequency = 300;

        /// <summary>
        /// The max retries to get well known keys from server.
        /// </summary>
        const int k_WellKnownKeysMaxAttempt = 3;

        readonly IAuthenticationNetworkClient m_NetworkClient;
        readonly IJwtDecoder m_JwtDecoder;
        readonly ICache m_Cache;
        readonly IScheduler m_Scheduler;
        readonly IDateTimeWrapper m_DateTime;
        readonly ILogger m_Logger;

        IWebRequest<SignInResponse> m_DelayedTokenRequest;
        string m_PlayerId;

        DateTime m_AccessTokenExpiryTime;

        internal event Action<AuthenticationState, AuthenticationState> StateChanged;

        public event Action<AuthenticationException> SignInFailed;
        public event Action SignedIn;
        public event Action SignedOut;

        public bool IsSignedIn =>
            State == AuthenticationState.Authorized ||
            State == AuthenticationState.Refreshing ||
            State == AuthenticationState.Expired;

        public bool IsAuthorized =>
            State == AuthenticationState.Authorized ||
            State == AuthenticationState.Refreshing;

        public string AccessToken { get; private set; }

        public string PlayerId =>

            // NOTE: player ID comes in with the authentication request, before the player has actually completed
            // the authorization process and signed in properly -- so make sure we don't accidentally expose the
            // player ID too early.
            IsSignedIn ? m_PlayerId : null;

        internal AuthenticationState State { get; set; }
        internal WellKnownKeys WellKnownKeys { get; private set; }

        internal AuthenticationServiceInternal(IAuthenticationNetworkClient networkClient,
                                               IJwtDecoder jwtDecoder,
                                               ICache cache,
                                               IScheduler scheduler,
                                               IDateTimeWrapper dateTime,
                                               ILogger logger)
        {
            m_NetworkClient = networkClient;
            m_JwtDecoder = jwtDecoder;
            m_Cache = cache;
            m_Scheduler = scheduler;
            m_DateTime = dateTime;
            m_Logger = logger;

            State = AuthenticationState.SignedOut;
        }

        void GetWellKnownKeys(AuthenticationAsyncOperation asyncOperation, int attempt)
        {
            if (WellKnownKeys == null)
            {
                var wellKnownKeysRequest = m_NetworkClient.GetWellKnownKeys();
                wellKnownKeysRequest.Completed += resp => WellKnownKeysReceived(asyncOperation, resp, attempt);
            }
        }

        internal void WellKnownKeysReceived(AuthenticationAsyncOperation asyncOperation, IWebRequest<WellKnownKeys> response, int attempt)
        {
            try
            {
                if (response.RequestFailed)
                {
                    if (attempt < k_WellKnownKeysMaxAttempt)
                    {
                        m_Logger.Warning($"Well-known keys request failed (attempt: {attempt}): {response.ResponseCode}, {response.ErrorMessage}");
                        GetWellKnownKeys(asyncOperation, attempt + 1);
                    }
                    else
                    {
                        HandleServerError(asyncOperation, response);
                    }

                    return;
                }

                m_Logger.Info("Well-known keys have arrived!");
                WellKnownKeys = response.ResponseBody;

                if (State == AuthenticationState.VerifyingAccessToken)
                {
                    CompleteSignIn(asyncOperation, m_DelayedTokenRequest.ResponseBody);
                }
            }
            catch (Exception e)
            {
                asyncOperation.Fail(AuthenticationError.UnknownError, "Unknown error receiving well-known keys response.", e);
            }
        }

        public Task SignInAnonymouslyAsync()
        {
            if (State == AuthenticationState.SignedOut)
            {
                if (!string.IsNullOrEmpty(ReadSessionToken()))
                {
                    return SignInWithSessionTokenAsync();
                }

                // I am a first-time anonymous user.
                return StartSigningIn(m_NetworkClient.SignInAnonymously()).AsTask();
            }

            return AlreadySignedInError().AsTask();
        }

        public Task SignInWithAppleAsync(string idToken)
        {
            return SignInWithExternalToken(new ExternalTokenRequest
            {
                IdProvider = k_IdProviderApple,
                Token = idToken
            }).AsTask();
        }

        public Task LinkWithAppleAsync(string idToken)
        {
            return LinkWithExternalToken(new ExternalTokenRequest
            {
                IdProvider = k_IdProviderApple,
                Token = idToken
            }).AsTask();
        }

        public Task SignInWithGoogleAsync(string idToken)
        {
            return SignInWithExternalToken(new ExternalTokenRequest
            {
                IdProvider = k_IdProviderGoogle,
                Token = idToken
            }).AsTask();
        }

        public Task LinkWithGoogleAsync(string idToken)
        {
            return LinkWithExternalToken(new ExternalTokenRequest
            {
                IdProvider = k_IdProviderGoogle,
                Token = idToken
            }).AsTask();
        }

        public Task SignInWithFacebookAsync(string accessToken)
        {
            return SignInWithExternalToken(new ExternalTokenRequest
            {
                IdProvider = k_IdProviderFacebook,
                Token = accessToken
            }).AsTask();
        }

        public Task LinkWithFacebookAsync(string accessToken)
        {
            return LinkWithExternalToken(new ExternalTokenRequest
            {
                IdProvider = k_IdProviderFacebook,
                Token = accessToken
            }).AsTask();
        }

        public Task SignInWithSteamAsync(string sessionTicket)
        {
            return SignInWithExternalToken(new ExternalTokenRequest
            {
                IdProvider = k_IdProviderSteam,
                Token = sessionTicket
            }).AsTask();
        }

        public Task LinkWithSteamAsync(string sessionTicket)
        {
            return LinkWithExternalToken(new ExternalTokenRequest
            {
                IdProvider = k_IdProviderSteam,
                Token = sessionTicket
            }).AsTask();
        }

        internal IAsyncOperation SignInWithExternalToken(ExternalTokenRequest externalToken)
        {
            if (State == AuthenticationState.SignedOut)
            {
                return StartSigningIn(m_NetworkClient.SignInWithExternalToken(externalToken));
            }

            return AlreadySignedInError();
        }

        internal IAsyncOperation LinkWithExternalToken(ExternalTokenRequest externalToken)
        {
            var operation = new AuthenticationAsyncOperation(m_Logger);
            if (IsAuthorized)
            {
                var linkResult = m_NetworkClient.LinkWithExternalToken(AccessToken, externalToken);
                linkResult.Completed += request => LinkResponseReceived(operation, request);
            }
            else
            {
                m_Logger.Warning("The player is not authorized. Wait until authorized before attempting to link.");
                operation.Fail(AuthenticationError.ClientInvalidUserState);
            }

            return operation;
        }

        public Task SignInWithSessionTokenAsync()
        {
            return SignInWithSessionToken(false).AsTask();
        }

        internal IAsyncOperation SignInWithSessionToken(bool isRefreshRequest)
        {
            var sessionToken = ReadSessionToken();
            if (State == AuthenticationState.SignedOut || isRefreshRequest)
            {
                if (string.IsNullOrEmpty(sessionToken))
                {
                    return SessionTokenNotExistsError();
                }

                m_Logger.Info("Continuing existing session with cached token.");

                return StartSigningIn(m_NetworkClient.SignInWithSessionToken(sessionToken), isRefreshRequest);
            }

            return AlreadySignedInError();
        }

        internal string ReadSessionToken()
        {
            return m_Cache.GetString(k_CacheKeySessionToken) ?? string.Empty;
        }

        IAsyncOperation StartSigningIn(IWebRequest<SignInResponse> signInRequest, bool isRefreshRequest = false)
        {
            var asyncOp = CreateSignInAsyncOperation();

            if (!isRefreshRequest)
            {
                ChangeState(AuthenticationState.SigningIn);
            }

            GetWellKnownKeys(asyncOp, 0);

            if (isRefreshRequest)
            {
                signInRequest.Completed += request => RefreshResponseReceived(request);
            }
            else
            {
                signInRequest.Completed += request => SignInResponseReceived(asyncOp, request);
            }

            return asyncOp;
        }

        public void SignOut()
        {
            if (State != AuthenticationState.SignedOut)
            {
                AccessToken = null;
                m_AccessTokenExpiryTime = default;
                m_PlayerId = null;

                m_Cache.DeleteKey(k_CacheKeySessionToken);
                m_Scheduler.CancelAction(ScheduledRefresh);

                ChangeState(AuthenticationState.SignedOut);
            }
        }

        internal void SignInResponseReceived(AuthenticationAsyncOperation operation, IWebRequest<SignInResponse> request)
        {
            try
            {
                if (HandleServerError(operation, request))
                {
                    return;
                }

                if (WellKnownKeys == null)
                {
                    m_Logger.Info("Well-known keys have not arrived yet, waiting on them to complete sign-in.");

                    m_DelayedTokenRequest = request;

                    ChangeState(AuthenticationState.VerifyingAccessToken);
                    // operation will complete in WellKnownKeysReceived()
                }
                else
                {
                    CompleteSignIn(operation, request.ResponseBody);
                }
            }
            catch (Exception e)
            {
                operation.Fail(AuthenticationError.UnknownError, "Unknown error receiving SignIn response.", e);
            }
        }

        internal void LinkResponseReceived(AuthenticationAsyncOperation operation, IWebRequest<SignInResponse> request)
        {
            try
            {
                if (HandleServerError(operation, request))
                {
                    return;
                }
                // The data in the response of link can be discarded. We already have all information in current context.
                // Just mark it as succeed to notify caller that the user is linked successfully.
                operation.Succeed();
            }
            catch (Exception e)
            {
                operation.Fail(AuthenticationError.UnknownError, "Unknown error receiving link response.", e);
            }
        }

        void CompleteSignIn(AuthenticationAsyncOperation operation, SignInResponse response)
        {
            try
            {
                var accessTokenDecoded = m_JwtDecoder.Decode<AccessToken>(response.IdToken, WellKnownKeys);
                if (accessTokenDecoded == null)
                {
                    operation.Fail(AuthenticationError.InvalidAccessToken, "Failed to decode and verify access token.");
                }
                else
                {
                    AccessToken = response.IdToken;
                    m_PlayerId = response.UserId;
                    m_Cache.SetString(k_CacheKeySessionToken, response.SessionToken);

                    var expiry = response.ExpiresIn > k_AccessTokenRefreshBuffer ? response.ExpiresIn - k_AccessTokenRefreshBuffer : response.ExpiresIn;

                    m_AccessTokenExpiryTime = m_DateTime.UtcNow.AddSeconds(response.ExpiresIn - k_AccessTokenExpiryBuffer);

                    m_Scheduler.ScheduleAction(ScheduledRefresh, expiry);

                    ChangeState(AuthenticationState.Authorized);

                    operation.Succeed();
                }
            }
            catch (Exception e)
            {
                operation.Fail(AuthenticationError.UnknownError, "Unknown error completing sign-in.", e);
            }
        }

        internal void ScheduledRefresh()
        {
            // If we have just been unpaused, Unity's execution order may mean that this triggers
            // the refresh process when in fact the Expiry process should take hold (i.e. the scheduler executes
            // this action before the OnApplicationPause callback). So to ensure we don't double the refresh
            // request, check the token hasn't expired before triggering refresh.
            if (m_AccessTokenExpiryTime > m_DateTime.UtcNow)
            {
                RefreshAccessToken();
            }
        }

        internal void RefreshAccessToken()
        {
            if (IsSignedIn)
            {
                m_Logger.Info("Making token refresh request...");

                if (State != AuthenticationState.Expired)
                {
                    ChangeState(AuthenticationState.Refreshing);
                }

                SignInWithSessionToken(true);
            }
        }

        internal void RefreshResponseReceived(IWebRequest<SignInResponse> request)
        {
            if (request.RequestFailed)
            {
                m_Logger.Error($"Request failed: {request.ResponseCode}, {request.ErrorMessage}");

                // NOTE: depending on how long it took to fail, this might occur before the access token has _actually_ expired.
                // This is likely safer than risking an expired access token reaching a consuming service.

                if (request.ServerError && request.ResponseCode < 500)
                {
                    m_Logger.Warning("Failed to refresh access token due to server error, signing out.");
                    SignOut();
                }
                else
                {
                    m_Logger.Warning("Failed to refresh access token due to network error or internal server error, will retry later.");

                    m_Scheduler.ScheduleAction(RefreshAccessToken, k_ExpiredRefreshAttemptFrequency);

                    if (State != AuthenticationState.Expired)
                    {
                        Expire();
                    }
                }
            }
            else
            {
                var asyncOp = new AuthenticationAsyncOperation(m_Logger);
                CompleteSignIn(asyncOp, request.ResponseBody);
                if (asyncOp.Status == AsyncOperationStatus.Succeeded)
                {
                    m_Logger.Info("Refresh complete!");
                }
                else
                {
                    m_Logger.Warning("The access token is not valid. Retry JWKS and refresh again.");

                    // Refresh failed since we received a bad token. Retry.
                    m_Scheduler.ScheduleAction(RefreshAccessToken, k_ExpiredRefreshAttemptFrequency);
                }
            }
        }

        public void ApplicationUnpaused()
        {
            if (IsAuthorized &&
                m_DateTime.UtcNow > m_AccessTokenExpiryTime)
            {
                m_Logger.Info("Application unpause found the access token to have expired already.");
                Expire();
                RefreshAccessToken();
            }
        }

        void Expire()
        {
            AccessToken = null;
            m_AccessTokenExpiryTime = default;

            ChangeState(AuthenticationState.Expired);
        }

        void ChangeState(AuthenticationState newState)
        {
            // NOTE: always call this at the end of a method where state is changed, so that any consumer
            // that has subscribed to the event will get the correct data for the new state.

            m_Logger.Info($"Moved from state [{State}] to [{newState}]");

            var oldState = State;
            State = newState;

            HandleStateChanged(oldState, newState);
        }

        void HandleStateChanged(AuthenticationState oldState, AuthenticationState newState)
        {
            StateChanged?.Invoke(oldState, newState);
            switch (newState)
            {
                case AuthenticationState.Authorized:
                    if (oldState != AuthenticationState.Refreshing &&
                        oldState != AuthenticationState.Expired)
                    {
                        SignedIn?.Invoke();
                    }

                    break;
                case AuthenticationState.SignedOut:
                    SignedOut?.Invoke();
                    break;
            }
        }

        /// <summary>
        /// Create <see cref="IAsyncOperation{T}"/> that always fail with <c>ClientInvalidUserState</c> error
        /// when the user is calling <c>SignIn*</c> methods while not in <c>SignedOut</c> state.
        /// </summary>
        /// <returns>The exception that represents the error.</returns>
        IAsyncOperation AlreadySignedInError()
        {
            var exception = new AuthenticationException(
                AuthenticationError.ClientInvalidUserState,
                "The player is already signed in. Sign out before attempting to sign in again.");
            m_Logger.Warning(exception.Message);

            var asyncOp = new AuthenticationAsyncOperation(null);
            asyncOp.Fail(exception);
            return asyncOp;
        }

        /// <summary>
        /// Create <see cref="IAsyncOperation{T}"/> that always fail with <c>ClientNoActiveSession</c> error
        /// when the user is calling <c>SignInWithSessionToken</c> methods while there is no session token stored.
        /// </summary>
        /// <returns>The exception that represents the error.</returns>
        IAsyncOperation SessionTokenNotExistsError()
        {
            var exception = new AuthenticationException(
                AuthenticationError.ClientNoActiveSession,
                "There is no cached session token.");
            m_Logger.Warning(exception.Message);

            // At this point, the contents of the cache are invalid, and we don't want future
            // SignInAnonymously or SignInWithSessionToken to read the current contents of the key.
            m_Cache.DeleteKey(k_CacheKeySessionToken);

            var asyncOp = new AuthenticationAsyncOperation(null);
            asyncOp.Fail(exception);
            return asyncOp;
        }

        /// <summary>
        /// Handles the error from server. If request is not failed, do nothing. Otherwise try to parse the error
        /// and call <c>operation.Fail</c>. Caller shall check <c>operation.Status</c> before moving forward.
        /// </summary>
        /// <param name="operation">The async operation to mark failure in case of server error.</param>
        /// <param name="request">The web request to parse error.</param>
        /// <typeparam name="T">The type parameter of web request. In case of an error it is not used.</typeparam>
        /// <returns>Whether there is an error occurred.</returns>
        bool HandleServerError<T>(AuthenticationAsyncOperation operation, IWebRequest<T> request)
        {
            if (!request.RequestFailed)
            {
                return false;
            }

            m_Logger.Error($"Request failed: {request.ResponseCode}, {request.ErrorMessage}");

            if (request.NetworkError)
            {
                operation.Fail(AuthenticationError.NetworkError);
                return true;
            }

            // otherwise it's a server error. Try to parse the error.
            try
            {
                var errorResponse = JsonConvert.DeserializeObject<AuthenticationErrorResponse>(request.ErrorMessage);
                operation.Fail(errorResponse.Title, errorResponse.Detail);
            }
            catch (JsonException ex)
            {
                operation.Fail(
                    AuthenticationError.UnknownError,
                    "Failed to deserialize server response.",
                    ex);
            }
            catch (Exception ex)
            {
                operation.Fail(
                    AuthenticationError.UnknownError,
                    "Unknown error deserializing server response. ",
                    ex);
            }

            return true;
        }

        /// <summary>
        /// Create the AsyncOperation that will sign-out in case of failure.
        /// </summary>
        internal AuthenticationAsyncOperation CreateSignInAsyncOperation()
        {
            var asyncOp = new AuthenticationAsyncOperation(m_Logger);
            asyncOp.BeforeFail += SendSignInFailedEvent;

            return asyncOp;
        }

        void SendSignInFailedEvent(AuthenticationAsyncOperation operation)
        {
            SignInFailed?.Invoke(operation.Exception);
            SignOut();
        }
    }
}
