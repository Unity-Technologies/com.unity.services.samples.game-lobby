using System;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

namespace LobbyRelaySample.lobby
{
    /// <summary>
    /// Convert the lobby resulting from a request into a LocalLobby for use in the game logic.
    /// </summary>
    public static class ToLocalLobby
    {
        /// <summary>
        /// Create a new LocalLobby from the content of a retrieved lobby. Its data can be copied into an existing LocalLobby for use.
        /// </summary>
        public static void Convert(Lobby lobby, LocalLobby outputToHere, LobbyUser existingLocalUser = null)
        {
            LobbyInfo info = new LobbyInfo
            {   LobbyID             = lobby.Id,
                LobbyCode           = lobby.LobbyCode,
                Private             = lobby.IsPrivate,
                LobbyName           = lobby.Name,
                MaxPlayerCount      = lobby.MaxPlayers,
                RelayCode           = lobby.Data?.ContainsKey("RelayCode") == true ? lobby.Data["RelayCode"].Value : null,
                State               = lobby.Data?.ContainsKey("State") == true ? (LobbyState) int.Parse(lobby.Data["State"].Value) : LobbyState.Lobby,
                AllPlayersReadyTime = lobby.Data?.ContainsKey("AllPlayersReady") == true ? long.Parse(lobby.Data["AllPlayersReady"].Value) : (long?)null
            };
            Dictionary<string, LobbyUser> lobbyUsers = new Dictionary<string, LobbyUser>();
            foreach (var player in lobby.Players)
            {
                if (existingLocalUser != null && player.Id.Equals(existingLocalUser.ID))
                {
                    existingLocalUser.IsHost = lobby.HostId.Equals(player.Id);
                    existingLocalUser.DisplayName = player.Data?.ContainsKey("DisplayName") == true ? player.Data["DisplayName"].Value : existingLocalUser.DisplayName;
                    existingLocalUser.Emote = player.Data?.ContainsKey("Emote") == true ? (EmoteType) int.Parse(player.Data["Emote"].Value) : existingLocalUser.Emote;
                    lobbyUsers.Add(existingLocalUser.ID, existingLocalUser);
                }
                else
                {
                    LobbyUser user = new LobbyUser(
                        displayName: player.Data?.ContainsKey("DisplayName") == true ? player.Data["DisplayName"].Value : "NewPlayer",
                        isHost: lobby.HostId.Equals(player.Id),
                        id: player.Id,
                        emote: player.Data?.ContainsKey("Emote") == true ? (EmoteType)int.Parse(player.Data["Emote"].Value) : EmoteType.None,
                        userStatus: player.Data?.ContainsKey("UserStatus") == true ? player.Data["UserStatus"].Value : UserStatus.Lobby.ToString()
                    );
                    lobbyUsers.Add(user.ID, user);
                }
            }

            outputToHere.CopyObserved(info, lobbyUsers);
        }

        /// <summary>
        /// Create a list of new LocalLobby from the content of a retrieved lobby.
        /// </summary>
        public static List<LocalLobby> Convert(QueryResponse response)
        {
            List<LocalLobby> retLst = new List<LocalLobby>();
            foreach (var lobby in response.Results)
                retLst.Add(Convert(lobby));
            return retLst;
        }
        private static LocalLobby Convert(Lobby lobby)
        {
            LocalLobby data = new LocalLobby();
            Convert(lobby, data, null);
            return data;
        }

        public static Dictionary<string, string> RetrieveLobbyData(LocalLobby lobby)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("RelayCode", lobby.RelayCode);
            data.Add("State", ((int)lobby.State).ToString());
            // We only want the ArePlayersReadyTime to be set when we actually are ready for it, and it's null otherwise. So, don't set that here.
            return data;
        }

        public static Dictionary<string, string> RetrieveUserData(LobbyUser user)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            if (user == null || string.IsNullOrEmpty(user.ID))
                return data;
            data.Add("DisplayName", user.DisplayName);
            data.Add("Emote", ((int)user.Emote).ToString());
            data.Add("UserStatus", user.UserStatus.ToString());
            return data;
        }
    }
}
