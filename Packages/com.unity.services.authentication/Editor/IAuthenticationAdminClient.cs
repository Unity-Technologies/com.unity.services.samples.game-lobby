using Unity.Services.Authentication.Editor.Models;
using Unity.Services.Core.Internal;

namespace Unity.Services.Authentication.Editor
{
    static class IdProviderType
    {
        public const string Apple = "apple.com";
        public const string Facebook = "facebook.com";
        public const string Steam = "steampowered.com";
        public const string Google = "google.com";

        public static readonly string[] All =
        {
            Apple,
            Facebook,
            Google,
            Steam
        };
    }

    interface IAuthenticationAdminClient
    {
        /// <summary>
        /// Get the ID domain associated with the project.
        /// </summary>
        /// <param name="projectId">The Unity project ID.</param>
        /// <returns>Async operation with the id domain ID as the result.</returns>
        IAsyncOperation<string> GetIDDomain();

        /// <summary>
        /// Lists all ID providers created for the organization's specified ID domain
        /// </summary>
        /// <param name="iddomain">The ID domain ID</param>
        /// <returns>The list of ID Providers configured in the ID domain.</returns>
        IAsyncOperation<ListIdProviderResponse> ListIdProviders(string iddomain);

        /// <summary>
        /// Create a new ID provider for the organization's specified ID domain
        /// </summary>
        /// <param name="iddomain">The ID domain ID</param>
        /// <param name="request">The ID provider to create.</param>
        /// <returns>The ID Provider created.</returns>
        IAsyncOperation<IdProviderResponse> CreateIdProvider(string iddomain, CreateIdProviderRequest request);

        /// <summary>
        /// Update an ID provider for the organization's specified ID domain
        /// </summary>
        /// <param name="iddomain">The ID domain ID</param>
        /// <param name="request">The ID provider to create.</param>
        /// <returns>The ID Provider updated.</returns>
        IAsyncOperation<IdProviderResponse> UpdateIdProvider(string iddomain, string type, UpdateIdProviderRequest request);

        /// <summary>
        /// Enable an ID provider for the organization's specified ID domain
        /// </summary>
        /// <param name="iddomain">The ID domain ID</param>
        /// <param name="type">The type of the ID provider.</param>
        /// <returns>The ID Provider updated.</returns>
        IAsyncOperation<IdProviderResponse> EnableIdProvider(string iddomain, string type);

        /// <summary>
        /// Disable an ID provider for the organization's specified ID domain
        /// </summary>
        /// <param name="iddomain">The ID domain ID</param>
        /// <param name="type">The type of the ID provider.</param>
        /// <returns>The ID Provider updated.</returns>
        IAsyncOperation<IdProviderResponse> DisableIdProvider(string iddomain, string type);

        /// <summary>
        /// Delete a specific ID provider from the organization's specified ID domain
        /// </summary>
        /// <param name="iddomain">The ID domain ID</param>
        /// <param name="type">The type of the ID provider.</param>
        /// <returns>The async operation to check whether the task is done.</returns>
        IAsyncOperation<IdProviderResponse> DeleteIdProvider(string iddomain, string type);
    }
}
