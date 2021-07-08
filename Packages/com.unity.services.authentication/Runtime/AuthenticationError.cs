using System;

namespace Unity.Services.Authentication
{
    /// <summary>
    /// AuthenticationError lists the error codes to expect from <c>AuthenticationException</c> and failed events.
    /// </summary>
    public static class AuthenticationError
    {
        /// <summary>
        /// This is a client error that is returned when the user is not in the right state.
        /// For example, calling SignOut when the user is already signed out will result in this error.
        /// </summary>
        public const string ClientInvalidUserState = "CLIENT_INVALID_USER_STATE";

        /// <summary>
        /// This is a client error that is returned when trying to sign-in with session token while there is no cached
        /// session token.
        /// </summary>
        public const string ClientNoActiveSession = "CLIENT_NO_ACTIVE_SESSION";

        /// <summary>
        /// The error returned when auth code parameter is not found in the authorize response.
        /// </summary>
        public const string AuthCodeNotFound = "AUTH_CODE_NOT_FOUND";

        /// <summary>
        /// The error returned when the access token returned by server is invalid.
        /// </summary>
        public const string InvalidAccessToken = "INVALID_ACCESS_TOKEN";

        /// <summary>
        /// The error returned when the entity with the same key already exists.
        /// It happens when a user tries to link a social account while the social account is already linked with another user.
        /// </summary>
        public const string EntityExists = "ENTITY_EXISTS";

        /// <summary>
        /// The error returned when the parameter is missing or not in the right format.
        /// </summary>
        public const string InvalidParameters = "INVALID_PARAMETERS";

        /// <summary>
        /// The error returned when the permission is denied using the token provided.
        /// </summary>
        public const string PermissionDenied = "PERMISSION_DENIED";

        /// <summary>
        /// This is a network error when calling APIs.
        /// </summary>
        public const string NetworkError = "NETWORK_ERROR";

        /// <summary>
        /// This is an unknown error. It happens when there is an unexpected server response.
        /// </summary>
        public const string UnknownError = "UNKNOWN_ERROR";
    }
}
