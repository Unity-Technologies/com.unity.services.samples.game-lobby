using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

namespace LobbyRelaySample.lobby
{
    /// <summary>
    /// QueryToLocalList the lobby resulting from a request into a LocalLobby for use in the game logic.
    /// </summary>
    public static class LobbyConverters
    {
        const string key_RelayCode = nameof(LocalLobby.RelayCode);
        const string key_RelayNGOCode = nameof(LocalLobby.RelayNGOCode);
        const string key_LobbyState = nameof(LocalLobby.LocalLobbyState);
        const string key_LobbyColor = nameof(LocalLobby.LocalLobbyColor);
        const string key_LastEdit = nameof(LocalLobby.LastUpdated);

        const string key_Displayname = nameof(LocalPlayer.DisplayName);
        const string key_Userstatus = nameof(LocalPlayer.UserStatus);
        const string key_Emote = nameof(LocalPlayer.Emote);

        public static Dictionary<string, string> LocalToRemoteData(LocalLobby lobby)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add(key_RelayCode, lobby.RelayCode.Value);
            data.Add(key_RelayNGOCode, lobby.RelayNGOCode.Value);
            data.Add(key_LobbyState,
                ((int)lobby.LocalLobbyState.Value)
                .ToString()); // Using an int is smaller than using the enum state's name.
            data.Add(key_LobbyColor, ((int)lobby.LocalLobbyColor.Value).ToString());
            data.Add(key_LastEdit, lobby.LastUpdated.Value.ToString());

            return data;
        }

        public static Dictionary<string, string> LocalToRemoteUserData(LocalPlayer user)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            if (user == null || string.IsNullOrEmpty(user.ID.Value))
                return data;
            data.Add(key_Displayname, user.DisplayName.Value);
            data.Add(key_Userstatus,
                ((int)user.UserStatus.Value)
                .ToString()); // Cheaper to send the string int of the enum over the string enum
            data.Add(key_Emote, (user.Emote).ToString());
            return data;
        }

        /// <summary>
        /// Create a new LocalLobby from the content of a retrieved lobby. Its data can be copied into an existing LocalLobby for use.
        /// </summary>
        public static void RemoteToLocal(Lobby remoteLobby, LocalLobby localLobby, bool allowSetLobbyChanged = true)
        {
            localLobby.CanSetChanged = allowSetLobbyChanged;
            localLobby.LobbyID.Value = remoteLobby.Id;
            localLobby.LobbyName.Value = remoteLobby.Name;
            localLobby.LobbyCode.Value = remoteLobby.LobbyCode;
            localLobby.Private.Value = remoteLobby.IsPrivate;
            localLobby.AvailableSlots.Value = remoteLobby.AvailableSlots;
            localLobby.MaxPlayerCount.Value = remoteLobby.MaxPlayers;
            localLobby.LastUpdated.Value = remoteLobby.LastUpdated.ToFileTimeUtc();

            //Custom Data Conversion
            localLobby.RelayCode.Value = remoteLobby.Data?.ContainsKey(key_RelayCode) == true
                ? remoteLobby.Data[key_RelayCode].Value
                : localLobby.RelayCode.Value;
            localLobby.RelayNGOCode.Value = remoteLobby.Data?.ContainsKey(key_RelayNGOCode) == true
                ? remoteLobby.Data[key_RelayNGOCode].Value
                : localLobby.RelayNGOCode.Value;
            localLobby.LocalLobbyState.Value = remoteLobby.Data?.ContainsKey(key_LobbyState) == true
                ? (LobbyState)int.Parse(remoteLobby.Data[key_LobbyState].Value)
                : LobbyState.Lobby;
            localLobby.LocalLobbyColor.Value = remoteLobby.Data?.ContainsKey(key_LobbyColor) == true
                ? (LobbyColor)int.Parse(remoteLobby.Data[key_LobbyColor].Value)
                : LobbyColor.None;

            List<string> remotePlayerIDs = new List<string>();
            foreach (var player in remoteLobby.Players)
            {
                var id = player.Id;
                remotePlayerIDs.Add(id);
                var isHost = remoteLobby.HostId.Equals(player.Id);
                var displayName = player.Data?.ContainsKey(key_Displayname) == true
                    ? player.Data[key_Displayname].Value
                    : default;
                var emote = player.Data?.ContainsKey(key_Emote) == true
                    ? (EmoteType)int.Parse(player.Data[key_Emote].Value)
                    : default;
                var userStatus = player.Data?.ContainsKey(key_Userstatus) == true
                    ? (UserStatus)int.Parse(player.Data[key_Userstatus].Value)
                    : UserStatus.Connecting;
                LocalPlayer localPlayer;

                //See if we have the remote player locally already
                if (localLobby.LocalPlayers.ContainsKey(player.Id))
                {
                    localPlayer = localLobby.LocalPlayers[player.Id];
                    localPlayer.ID.Value = id;
                    localPlayer.DisplayName.Value = displayName;
                    localPlayer.Emote.Value = emote;
                    localPlayer.UserStatus.Value = userStatus;
                }
                else
                {
                    localPlayer = new LocalPlayer(id, isHost, displayName, emote, userStatus);
                    localLobby.AddPlayer(localPlayer);
                }
            }

            var disconnectedUsers = new List<LocalPlayer>();
            foreach (var (id, player) in localLobby.LocalPlayers)
            {
                if (!remotePlayerIDs.Contains(id))
                    disconnectedUsers.Add(player);
            }

            foreach (var remove in disconnectedUsers)
            {
                localLobby.RemovePlayer(remove);
            }

            localLobby.CanSetChanged = true;
        }

        /// <summary>
        /// Create a list of new LocalLobbies from the result of a lobby list query.
        /// </summary>
        public static List<LocalLobby> QueryToLocalList(QueryResponse response)
        {
            List<LocalLobby> retLst = new List<LocalLobby>();
            foreach (var lobby in response.Results)
                retLst.Add(RemoteToNewLocal(lobby));
            return retLst;
        }

        static LocalLobby RemoteToNewLocal(Lobby lobby)
        {
            LocalLobby data = new LocalLobby();
            RemoteToLocal(lobby, data);
            return data;
        }
    }
}