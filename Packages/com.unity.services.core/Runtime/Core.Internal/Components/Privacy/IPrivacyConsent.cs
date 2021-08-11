using System;
using Unity.Services.Core.Internal;

namespace Unity.Services.Privacy.Internal
{
    /// <summary>
    /// Contract for objects providing information regarding consent status.
    /// </summary>
    public interface IPrivacyConsent : IServiceComponent
    {
        /// <summary>
        /// Returns true when a a legislation currently applies for a given user
        /// </summary>
        /// <param name="legislation">The legislation to check</param>
        /// <returns><c>true</c> if the legislation applies for the user</returns>
        bool DoesLegislationApply(Legislation legislation);

        /// <summary>
        /// Current ConsentStatus for a given legislation based on the age gate requirements and the user's consent status.
        /// </summary>
        /// <param name="legislation">The legislation for which to get the consent status</param>
        /// <returns><c>ConsentStatus</c> for the requested legislation</returns>
        ConsentStatus UserConsentStatus(Legislation legislation);

        /// <summary>
        /// Event raised when the consent status has changed.
        /// </summary>
        event Action ConsentStatusChanged;
    }
}
