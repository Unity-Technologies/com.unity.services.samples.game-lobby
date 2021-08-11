using System;
using System.Threading.Tasks;

namespace Unity.Services.Authentication
{
    /// <summary>
    /// The functions for Authentication service.
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Whether the player is signed in or not.
        /// </summary>
        bool IsSignedIn { get; }

        /// <summary>
        /// Returns the access token if the current player is signed in, otherwise null.
        /// </summary>
        string AccessToken { get; }

        /// <summary>
        /// Returns the player's ID if the current player is signed in, otherwise null.
        /// </summary>
        string PlayerId { get; }

        /// <summary>
        /// Invoked when a sign-in attempt has completed successfully.
        /// </summary>
        event Action SignedIn;

        /// <summary>
        /// Invoked when a sign-out attempt has completed successfully.
        /// </summary>
        event Action SignedOut;

        /// <summary>
        /// Invoked when a sign-in attempt has failed, giving the error as the
        /// <see cref="AuthenticationException"/> parameter.
        /// </summary>
        event Action<AuthenticationException> SignInFailed;

        /// <summary>
        /// Sign the player in anonymously. No credentials are required and the session is confined to the current device.
        /// </summary>
        /// <remarks>
        /// If player has already signed in previously with a session token stored on the device, it signs the player back in no matter whether it's an anonymous player or not.
        /// </remarks>
        /// <returns>Task for the async operation</returns>
        /// <exception cref="AuthenticationException">An exception containing the message and ErrorCode of the error. Refer to <see cref="AuthenticationError"/> for error codes.</exception>
        Task SignInAnonymouslyAsync();

        /// <summary>
        /// Sign the player in with the session token stored on the device.
        /// </summary>
        /// <returns>Task for the async operation</returns>
        /// <exception cref="AuthenticationException">An exception containing the message and ErrorCode of the error. Refer to <see cref="AuthenticationError"/> for error codes.</exception>
        Task SignInWithSessionTokenAsync();

        /// <summary>
        /// Sign in using Apple's ID token.
        /// </summary>
        /// <param name="idToken">Apple's ID token</param>
        /// <returns>Task for the async operation</returns>
        /// <exception cref="AuthenticationException">An exception containing the message and ErrorCode of the error. Refer to <see cref="AuthenticationError"/> for error codes.</exception>
        Task SignInWithAppleAsync(string idToken);

        /// <summary>
        /// Link the current player with Apple account using Apple's ID token.
        /// </summary>
        /// <param name="idToken">Apple's ID token</param>
        /// <returns>Task for the async operation</returns>
        /// <exception cref="AuthenticationException">An exception containing the message and ErrorCode of the error. Refer to <see cref="AuthenticationError"/> for error codes.</exception>
        Task LinkWithAppleAsync(string idToken);

        /// <summary>
        /// Sign in using Google's ID token.
        /// </summary>
        /// <param name="idToken">Google's ID token</param>
        /// <returns>Task for the async operation</returns>
        /// <exception cref="AuthenticationException">An exception containing the message and ErrorCode of the error. Refer to <see cref="AuthenticationError"/> for error codes.</exception>
        Task SignInWithGoogleAsync(string idToken);

        /// <summary>
        /// Link the current player with Google account using Google's ID token.
        /// </summary>
        /// <param name="idToken">Google's ID token</param>
        /// <returns>Task for the async operation</returns>
        /// <exception cref="AuthenticationException">An exception containing the message and ErrorCode of the error. Refer to <see cref="AuthenticationError"/> for error codes.</exception>
        Task LinkWithGoogleAsync(string idToken);

        /// <summary>
        /// Sign in using Facebook's access token.
        /// </summary>
        /// <param name="accessToken">Facebook's access token</param>
        /// <returns>Task for the async operation</returns>
        /// <exception cref="AuthenticationException">An exception containing the message and ErrorCode of the error. Refer to <see cref="AuthenticationError"/> for error codes.</exception>
        Task SignInWithFacebookAsync(string accessToken);

        /// <summary>
        /// Link the current player with Facebook account using Facebook's access token.
        /// </summary>
        /// <param name="accessToken">Facebook's access token</param>
        /// <returns>Task for the async operation</returns>
        /// <exception cref="AuthenticationException">An exception containing the message and ErrorCode of the error. Refer to <see cref="AuthenticationError"/> for error codes.</exception>
        Task LinkWithFacebookAsync(string accessToken);

        /// <summary>
        /// Sign in using Steam's session ticket.
        /// </summary>
        /// <param name="sessionTicket">Steam's session ticket</param>
        /// <returns>Task for the async operation</returns>
        /// <exception cref="AuthenticationException">An exception containing the message and ErrorCode of the error. Refer to <see cref="AuthenticationError"/> for error codes.</exception>
        Task SignInWithSteamAsync(string sessionTicket);

        /// <summary>
        /// Link the current player with Steam account using Steam's session ticket.
        /// </summary>
        /// <param name="sessionTicket">Steam's session ticket</param>
        /// <returns>Task for the async operation</returns>
        /// <exception cref="AuthenticationException">An exception containing the message and ErrorCode of the error. Refer to <see cref="AuthenticationError"/> for error codes.</exception>
        Task LinkWithSteamAsync(string sessionTicket);

        /// <summary>
        /// Sign the current player out.
        /// </summary>
        /// <exception cref="AuthenticationException">An exception containing the message and ErrorCode of the error. Refer to <see cref="AuthenticationError"/> for error codes.</exception>
        void SignOut();

        /// <summary>
        /// The function to call when application is unpaused.
        /// It triggers and access token refresh if needed.
        /// </summary>
        void ApplicationUnpaused();
    }
}
