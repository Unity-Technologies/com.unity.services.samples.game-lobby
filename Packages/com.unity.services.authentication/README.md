# Unity Services Authentication SDK

This package provides a system for working with the Unity User Authentication Service (UAS), including log-in, player ID and access token retrieval, and session persistence.

## Integration

The Authentication SDK is currently available on the UPM Candidates Repository. You will need to add the UPM Candidates Registry (https://artifactory.prd.it.unity3d.com/artifactory/api/npm/upm-candidates) as a Scoped Registry to your project. Once you have done that, you can add the package `com.unity.services.authentication` with the latest version: `0.7.1-preview`.

Once you have installed the Authentication package, you must link your Unity project to a Unity Cloud Project using the Services window.

The Authentication SDK automatically initializes itself on game start (no prefabs are required, and it will initialize regardless of the current scene), so the only integration steps are to start using the Authentication API in your code. The API is exposed via the `Authentication.Instance` object in the `Unity.Services.Authentication` namespace.

Once the player has been signed in, the Authentication SDK will monitor the expiration time of their access token and attempt to refresh it automatically. No further action is required.

On starting a game you may notice a `UnityServicesContainer` game object is created in the DontDestroyOnLoad area, with an Authentication component. This is how the Authentication SDK hooks onto the Unity lifecycle events that it requires, so if you destroy this object or any of its components, the Authentication system will cease to function. Some values are also cached into `PlayerPrefs`, so clearing all `PlayerPrefs` keys will require the player to sign in again from scratch on their next session rather than being able to continue their current session.

## Public API

### Sign-In API

* `AuthenticationService.Instance.SignInAnonymouslyAsync()`
    * This triggers the anonymous sign-in processes, which may take some seconds to finish
	* This requires no parameters
	* Anonymous sign-in stores the Session Token in Unity PlayerPrefs until an explicit SignOut call is made, so if you simply quit the game before, anonymous sign-in can use the Session Token to let the same anonymous user continue
	* If you attempt to sign in while already signed in, this method will deliver a warning and set the async operation to fail with `AuthenticationError.ClientInvalidUserState`.
* `AuthenticationService.Instance.SignInWithSessionTokenAsync()`
    * This triggers the sign-in of the user with the session token stored on the device.
    * If there is no cached session token, the async operation fails with `AuthenticationError.ClientNoActiveSession`.
    * If you attempt to sign in while already signed in, this method will deliver a warning and set the async operation to fail with `AuthenticationError.ClientInvalidUserState`.
* `AuthenticationService.Instance.SignInWithAppleAsync(string idToken)`
    * This triggers the sign-in of the user with an ID token from Apple.
    * Game developer is responsible for installing the necessary SDK and get the token from Apple.
    * If you attempt to sign in while already signed in, this method will deliver a warning and set the async operation to fail with `AuthenticationError.ClientInvalidUserState`.
* `AuthenticationService.Instance.SignInWithGoogleAsync(string idToken)`
    * This triggers the sign-in of the user with an ID token from Google.
    * Game developer is responsible for installing the necessary SDK and get the token from Google.
    * If you attempt to sign in while already signed in, this method will deliver a warning and set the async operation to fail with `AuthenticationError.ClientInvalidUserState`.
* `AuthenticationService.Instance.SignInWithFacebookAsync(string accessToken)`
    * This triggers the sign-in of the user with an access token from Facebook.
    * Game developer is responsible for installing the necessary SDK and get the token from Facebook.
    * If you attempt to sign in while already signed in, this method will deliver a warning and set the async operation to fail with `AuthenticationError.ClientInvalidUserState`.
* `AuthenticationService.Instance.SignedIn`
	* This is an event to which you can subscribe to be notified when the sign-in process has completed successfully
* `AuthenticationService.Instance.SignInFailed`
	* This is an event to which you can subscribe to be notified when the sign-in process has failed for some reason
* `AuthenticationService.Instance.SignedOut`
	* This is an event to which you can subscribe to be notified when the user has been signed out for some reason (either because `AuthenticationService.Instance.SignOut()` was called explicitly, or because of a rejection in an automatic system e.g. a refresh attempt was rejected for having an invalid token)
* `AuthenticationService.Instance.SignOut()`
	* This triggers the sign-out process, which includes flushing all cached data and revocation of the access token
	* If you are not signed in, this method will do nothing
* `AuthenticationService.Instance.LinkWithAppleAsync(string idToken)`
    * This function links the current user with an ID token from Apple. The user can later sign-in with the linked Apple account.
    * Game developer is responsible for installing the necessary SDK and get the token from Apple.
    * If you attempt to link with an account that is already linked with another user, the async operation to fail with `AuthenticationError.EntityExists`.
    * If you attempt to sign in while already signed in, this method will deliver a warning and set the async operation to fail with `AuthenticationError.ClientInvalidUserState`.
* `AuthenticationService.Instance.LinkWithGoogleAsync(string idToken)`
    * This function links the current user with an ID token from Google. The user can later sign-in with the linked Google account.
    * Game developer is responsible for installing the necessary SDK and get the token from Google.
    * If you attempt to link with an account that is already linked with another user, the async operation to fail with `AuthenticationError.EntityExists`.
    * If you attempt to sign in while already signed in, this method will deliver a warning and set the async operation to fail with `AuthenticationError.ClientInvalidUserState`.
* `AuthenticationService.Instance.LinkWithFacebookAsync(string accessToken)`
    * This function links the current user with an access token from Facebook. The user can later sign-in with the linked Facebook account.
    * Game developer is responsible for installing the necessary SDK and get the token from Facebook.
    * If you attempt to link with an account that is already linked with another user, the async operation to fail with `AuthenticationError.EntityExists`.
    * If you attempt to sign in while already signed in, this method will deliver a warning and set the async operation to fail with `AuthenticationError.ClientInvalidUserState`.
* `AuthenticationService.Instance.IsSignedIn`
	* Returns true if the player is signed in
	* Note that the player is still considered signed in until they explicitly call SignOut (or some automatic process causes explicit sign-out), so this will return true even if the access token has expired
* `AuthenticationService.Instance.PlayerId`
	* This property exposes the ID of the player when they are signed in, or null if they are not
* `AuthenticationService.Instance.AccessToken`
	* Returns the raw string of the current access token, or null if no valid token is available (e.g. the player is signed in but the token has expired and could not be refreshed)
	* This value is updated automatically by the refresh process, so consumers should NOT cache this value

### Additional Methods

* `AuthenticationService.Instance.SetLogLevel(LogLevel level)`
	* This enables verbose logging by the Authentication SDK, including exposing the underlying state machine transitions, web request successes and other details that might assist debugging
	* By default this is set to Errors Only
