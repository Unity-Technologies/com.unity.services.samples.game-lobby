using Unity.Services.Core;

namespace Unity.Services.Authentication
{
    /// <summary>
    /// Contract for objects providing an access token to access remote services.
    /// </summary>
    public interface IAccessToken : IServiceComponent
    {
        /// <summary>
        /// The current token to use to access remote services.
        /// </summary>
        string AccessToken { get; }
    }
}
