namespace Unity.Services.Privacy
{
    /// <summary>
    /// Status of consent far a legislation
    /// </summary>
    public enum ConsentStatus
    {
        /// <summary>
        /// There is no consent status in storage
        /// </summary>
        NotRequested,
        /// <summary>
        /// The user has opted in or not expressively opted out when given the option
        /// </summary>
        OptedIn,
        /// <summary>
        /// The user is either under the age of consent or has expressively opted out
        /// </summary>
        OptedOut,
    }
}
