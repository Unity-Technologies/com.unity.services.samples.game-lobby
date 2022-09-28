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
            await UnityServices.InitializeAsync();

            #if UNITY_EDITOR
            if (ParrelSync.ClonesManager.IsClone())
            {
                // When using a ParrelSync clone, we'll automatically switch to a different authentication profile.
                // This will cause the clone to sign in as a different anonymous user account.  If you're going to use
                // authentication profiles for some other purpose, you may need to change the profile name.
                string customArgument = ParrelSync.ClonesManager.GetArgument();
                AuthenticationService.Instance.SwitchProfile($"Clone_{customArgument}_Profile");
            }
            #endif

            AuthenticationService.Instance.SignedIn += OnSignInChange;
            AuthenticationService.Instance.SignedOut += OnSignInChange;

            try
            {   if (!AuthenticationService.Instance.IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync(); // Don't sign out later, since that changes the anonymous token, which would prevent the player from exiting lobbies they're already in.
                onSigninComplete?.Invoke();
            }
            catch
            {   UnityEngine.Debug.LogError("Login failed. Did you remember to set your Project ID under Edit > Project Settings... > Services?");
                throw;
            }

            // Note: If for some reason your login state gets weird, you can comment out the previous block and instead call AuthenticationService.Instance.SignOut().
            // Then, running Play mode will fail to actually function and instead will log out of your previous anonymous account.
            // When you revert that change and run Play mode again, you should be logged in as a new anonymous account with a new default name.
        }

        private void OnSignInChange()
        {
            SetContent("id", AuthenticationService.Instance.PlayerId);
        }
    }
}
