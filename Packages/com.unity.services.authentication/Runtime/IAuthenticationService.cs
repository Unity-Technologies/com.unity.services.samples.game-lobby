using System;
using System.Threading.Tasks;

namespace Unity.Services.Authentication
{
    public interface IAuthenticationService
    {
        bool IsSignedIn { get; }
        string AccessToken { get; }
        string PlayerId { get; }

        event Action SignedIn;
        event Action SignedOut;
        event Action<AuthenticationException> SignInFailed;

        Task SignInAnonymouslyAsync();
        Task SignInWithSessionTokenAsync();

        Task SignInWithAppleAsync(string idToken);
        Task LinkWithAppleAsync(string idToken);

        Task SignInWithGoogleAsync(string idToken);
        Task LinkWithGoogleAsync(string idToken);

        Task SignInWithFacebookAsync(string accessToken);
        Task LinkWithFacebookAsync(string accessToken);

        Task SignInWithSteamAsync(string sessionTicket);
        Task LinkWithSteamAsync(string sessionTicket);

        void SignOut();

        void ApplicationUnpaused();
    }
}
