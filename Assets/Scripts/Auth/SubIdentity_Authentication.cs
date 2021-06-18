using System;
using Unity.Services.Authentication;
using Unity.Services.Core;

namespace LobbyRooms.Auth
{
    /// <summary>
    /// The Authentication package will sign in asynchronously and anonymously. When complete, we will need to store the generated ID.
    /// </summary>
    public class SubIdentity_Authentication : SubIdentity, IDisposable
    {
        private bool m_hasDisposed = false;

        /// <summary>
        /// This will kick off a login.
        /// </summary>
        public SubIdentity_Authentication(Action onSigninComplete = null)
        {
            DoSignIn(onSigninComplete);
        }
        ~SubIdentity_Authentication()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (!m_hasDisposed)
            {
                AuthenticationService.Instance.SignedIn -= OnSignInChange;
                AuthenticationService.Instance.SignedOut -= OnSignInChange;
                m_hasDisposed = true;
            }
        }

        private async void DoSignIn(Action onSigninComplete)
        {
            // TODO - this should probably be moved into general startup logic somewhere
            await UnityServices.Initialize();

            AuthenticationService.Instance.SignedIn += OnSignInChange;
            AuthenticationService.Instance.SignedOut += OnSignInChange;

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
//            Authentication.SignOut(); // TODO: I think we want to sign out at *some* point? But then the UAS anonymous token changes, so they can't access any outstanding rooms they've created.
            onSigninComplete?.Invoke();
        }

        private void OnSignInChange()
        {
            SetContent("id", AuthenticationService.Instance.PlayerId);
        }
    }
}
