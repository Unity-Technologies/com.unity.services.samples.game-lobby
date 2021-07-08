using Unity.Services.Lobbies.Apis;


namespace Unity.Services.Lobbies
{
    public static class LobbyService
    {
        /// <summary>
        /// Static accessor for LobbyApi methods.
        /// </summary>
        public static ILobbyApiClient LobbyApiClient { get; internal set; }
        
        public static Configuration Configuration = new Configuration("https://lobby.cloud.unity3d.com/v1", 10, 4, null);
    }
}
