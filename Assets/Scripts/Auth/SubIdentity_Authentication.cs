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
            Authentication.SetLogLevel(Unity.Services.Authentication.Utilities.LogLevel.Verbose);
            Authentication.SignedIn += OnSignInChange;
            Authentication.SignedOut += OnSignInChange;
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
                Authentication.SignedIn -= OnSignInChange;
                Authentication.SignedOut -= OnSignInChange;
                m_hasDisposed = true;
            }
        }

        private async void DoSignIn(Action onSigninComplete)
        {
            await UnityServices.Initialize();
            await Authentication.SignInAnonymously();
//            Authentication.SignOut(); // TODO: I think we want to sign out at *some* point? But then the UAS anonymous token changes, so they can't access any outstanding rooms they've created.
            onSigninComplete?.Invoke();
        }

        private void OnSignInChange()
        {
            SetContent("id", Authentication.PlayerId);
        }
    }
}
