using Unity.Services.Relay.Apis.Allocations;


namespace Unity.Services.Relay
{
    public static class RelayService
    {
        /// <summary>
        /// Static accessor for AllocationsApi methods.
        /// </summary>
        public static IAllocationsApiClient AllocationsApiClient { get; internal set; }
        
        public static Configuration Configuration = new Configuration("https://relay-allocations.services.api.unity.com", 10, 4, null);
    }
}
