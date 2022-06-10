using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

namespace LobbyRelaySample.lobby
{
    /// <summary>
    /// QueryToLocalList the lobby resulting from a request into a LocalLobby for use in the game logic.
    /// </summary>
    public static class LobbyConverters
    {
        public static Dictionary<string, string> LocalToRemoteData(LocalLobby lobby)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("RelayCode", lobby.RelayCode);
            data.Add("RelayNGOCode", lobby.RelayNGOCode);
            data.Add("State", ((int)lobby.State).ToString()); // Using an int is smaller than using the enum state's name.
            data.Add("Color", ((int)lobby.Color).ToString());
            data.Add("State_LastEdit", lobby.Data.State_LastEdit.ToString());
            data.Add("Color_LastEdit", lobby.Data.Color_LastEdit.ToString());
            data.Add("RelayNGOCode_LastEdit", lobby.Data.RelayNGOCode_LastEdit.ToString());
            return data;
        }

        public static Dictionary<string, string> LocalToRemoteUserData(LobbyUser user)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            if (user == null || string.IsNullOrEmpty(user.ID))
                return data;
            data.Add("DisplayName", user.DisplayName); // The lobby doesn't need to know any data beyond the name and state; Relay will handle the rest.
            data.Add("UserStatus", ((int)user.UserStatus).ToString());
            data.Add("Emote", ((int)user.Emote).ToString());
            return data;
        }

        /// <summary>
        /// Create a new LocalLobby from the content of a retrieved lobby. Its data can be copied into an existing LocalLobby for use.
        /// </summary>
        public static void RemoteToLocal(Lobby remoteLobby, LocalLobby localLobbyToUpdate)
        {
            //Copy Data from Lobby into Local lobby fields
            LocalLobby.LobbyData lobbyData = new LocalLobby.LobbyData(localLobbyToUpdate.Data)
            {
                LobbyID = remoteLobby.Id,
                LobbyCode = remoteLobby.LobbyCode,
                Private = remoteLobby.IsPrivate,
                LobbyName = remoteLobby.Name,
                MaxPlayerCount = remoteLobby.MaxPlayers,
                State_LastEdit = remoteLobby.LastUpdated.Ticks,
                RelayCode = remoteLobby.Data?.ContainsKey("RelayCode") == true ? remoteLobby.Data["RelayCode"].Value : localLobbyToUpdate.RelayCode, // By providing RelayCode through the lobby data with Member visibility, we ensure a client is connected to the lobby before they could attempt a relay connection, preventing timing issues between them.
                RelayNGOCode = remoteLobby.Data?.ContainsKey("RelayNGOCode") == true ? remoteLobby.Data["RelayNGOCode"].Value : localLobbyToUpdate.RelayNGOCode,
                State = remoteLobby.Data?.ContainsKey("State") == true ? (LobbyState)int.Parse(remoteLobby.Data["State"].Value) : LobbyState.Lobby,
                Color = remoteLobby.Data?.ContainsKey("Color") == true ? (LobbyColor)int.Parse(remoteLobby.Data["Color"].Value) : LobbyColor.None,
                Color_LastEdit = remoteLobby.Data?.ContainsKey("Color_LastEdit") == true ? long.Parse(remoteLobby.Data["Color_LastEdit"].Value) : localLobbyToUpdate.Data.Color_LastEdit,
                RelayNGOCode_LastEdit = remoteLobby.Data?.ContainsKey("RelayNGOCode_LastEdit") == true ? long.Parse(remoteLobby.Data["RelayNGOCode_LastEdit"].Value) : localLobbyToUpdate.Data.RelayNGOCode_LastEdit
            };

            Dictionary<string, LobbyUser> lobbyUsers = new Dictionary<string, LobbyUser>();
            foreach (var player in remoteLobby.Players)
            {
                // (If we haven't seen this player yet, a new local representation of the player will have already been added by the LocalLobby.)
                LobbyUser incomingData = new LobbyUser
                {
                    IsHost = remoteLobby.HostId.Equals(player.Id),
                    DisplayName = player.Data?.ContainsKey("DisplayName") == true ? player.Data["DisplayName"].Value : default,
                    Emote = player.Data?.ContainsKey("Emote") == true ? (EmoteType)int.Parse(player.Data["Emote"].Value) : default,
                    UserStatus = player.Data?.ContainsKey("UserStatus") == true ? (UserStatus)int.Parse(player.Data["UserStatus"].Value) : UserStatus.Connecting,
                    ID = player.Id
                };
                lobbyUsers.Add(incomingData.ID, incomingData);
            }

            //Push all the data at once so we don't call OnChanged for each variable
            localLobbyToUpdate.CopyObserved(lobbyData, lobbyUsers);
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
