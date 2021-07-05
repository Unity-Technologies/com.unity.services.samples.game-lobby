using System;
using Unity.Services.Authentication;
using Unity.Services.Core;

namespace LobbyRelaySample.Auth
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
            await UnityServices.Initialize();
            AuthenticationService.Instance.SignedIn += OnSignInChange;
            AuthenticationService.Instance.SignedOut += OnSignInChange;

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync(); // Note: We don't want to sign out later, since that changes the UAS anonymous token, which would prevent the player from exiting rooms they're already in.
            onSigninComplete?.Invoke();
        }

        private void OnSignInChange()
        {
            SetContent("id", AuthenticationService.Instance.PlayerId);
        }
    }
}
