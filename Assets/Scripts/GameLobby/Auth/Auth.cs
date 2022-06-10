using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace LobbyRelaySample
{
    public enum AuthState
    {
        Initialized,
        Authenticating,
        Authenticated,
        Error,
        TimedOut
    }

    public static class Auth
    {
        public static AuthState AuthenticationState { get; private set; } = AuthState.Initialized;

        public static async Task<AuthState> Authenticate(string profile,int tries = 5)
        {
            //If we are already authenticated, just return Auth
            if (AuthenticationState == AuthState.Authenticated)
            {
                return AuthenticationState;
            }

            if (AuthenticationState == AuthState.Authenticating)
            {
                Debug.LogWarning("Cant Authenticate if we are authenticating or authenticated");
                await Authenticating();
                return AuthenticationState;
            }

            var profileOptions = new InitializationOptions();
            profileOptions.SetProfile(profile);
            await UnityServices.InitializeAsync(profileOptions);
            await SignInAnonymouslyAsync(tries);
            Debug.Log($"Auth attempts Finished : {AuthenticationState.ToString()}");

            return AuthenticationState;
        }

        //Awaitable task that will pass the clientID once authentication is done.
        public static string ID()
        {
            return AuthenticationService.Instance.PlayerId;
        }

        //Awaitable task that will pass once authentication is done.
        public static async Task<AuthState> Authenticating()
        {
            while (AuthenticationState == AuthState.Authenticating || AuthenticationState == AuthState.Initialized)
            {
                await Task.Delay(200);
            }

            return AuthenticationState;
        }

        public static bool DoneAuthenticating()
        {
            return AuthenticationState != AuthState.Authenticating &&
                   AuthenticationState != AuthState.Initialized;
        }

        static async Task SignInAnonymouslyAsync(int maxRetries)
        {
            AuthenticationState = AuthState.Authenticating;
            var tries = 0;
            while (AuthenticationState == AuthState.Authenticating && tries < maxRetries)
            {
                try
                {

                    //To ensure staging login vs non staging
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();

                    if (AuthenticationService.Instance.IsSignedIn && AuthenticationService.Instance.IsAuthorized)
                    {
                        AuthenticationState = AuthState.Authenticated;
                        break;
                    }
                }
                catch (AuthenticationException ex)
                {
                    // Compare error code to AuthenticationErrorCodes
                    // Notify the player with the proper error message
                    Debug.LogError(ex);
                    AuthenticationState = AuthState.Error;
                }
                catch (RequestFailedException exception)
                {
                    // Compare error code to CommonErrorCodes
                    // Notify the player with the proper error message
                    Debug.LogError(exception);
                    AuthenticationState = AuthState.Error;
                }

                tries++;
                await Task.Delay(1000);
            }

            if (AuthenticationState != AuthState.Authenticated)
            {
                Debug.LogWarning($"Player was not signed in successfully after {tries} attempts");
                AuthenticationState = AuthState.TimedOut;
            }
        }

        public static void SignOut()
        {
            AuthenticationService.Instance.SignOut(false);
            AuthenticationState = AuthState.Initialized;
        }
    }
}
