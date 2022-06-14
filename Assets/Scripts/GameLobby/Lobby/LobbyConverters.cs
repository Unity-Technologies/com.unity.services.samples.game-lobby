using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

namespace LobbyRelaySample.lobby
{
    /// <summary>
    /// QueryToLocalList the lobby resulting from a request into a LocalLobby for use in the game logic.
    /// </summary>
    public static class LobbyConverters
    {
        const string key_RelayCode = nameof(LocalLobby.LobbyData.RelayCode);
        const string key_RelayNGOCode = nameof(LocalLobby.LobbyData.RelayNGOCode);
        const string key_LobbyState = nameof(LocalLobby.LobbyData.LobbyState);
        const string key_LobbyColor = nameof(LocalLobby.LobbyData.LobbyColor);
        const string key_LastEdit = nameof(LocalLobby.LobbyData.LastEdit);

        const string key_Displayname = nameof(LobbyUser.DisplayName);
        const string key_Userstatus = nameof(LobbyUser.UserStatus);
        const string key_Emote = nameof(LobbyUser.Emote);


        public static Dictionary<string, string> LocalToRemoteData(LocalLobby lobby)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add(key_RelayCode, lobby.RelayCode.Value);
            data.Add(key_RelayNGOCode, lobby.RelayNGOCode.Value);
            data.Add(key_LobbyState, ((int)lobby.LobbyState).ToString()); // Using an int is smaller than using the enum state's name.
            data.Add(key_LobbyColor, ((int)lobby.LobbyColor).ToString());
            data.Add(key_LastEdit, lobby.Data.LastEdit.ToString());

            return data;
        }

        public static Dictionary<string, string> LocalToRemoteUserData(LobbyUser user)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            if (user == null || string.IsNullOrEmpty(user.ID))
                return data;
            data.Add(key_Displayname, user.DisplayName);
            data.Add(key_Userstatus, ((int)user.UserStatus).ToString());
            data.Add(key_Emote , ((int)user.Emote).ToString());
            return data;
        }

        /// <summary>
        /// Create a new LocalLobby from the content of a retrieved lobby. Its data can be copied into an existing LocalLobby for use.
        /// </summary>
        public static void RemoteToLocal(Lobby remoteLobby, LocalLobby localLobby)
        {


            localLobby.LobbyID.Value = remoteLobby.Id;
            localLobby.LobbyCode.Value = remoteLobby.LobbyCode;
            localLobby.RelayCode.Value = remoteLobby.Data?.ContainsKey(key_RelayCode) == true
                ? remoteLobby.Data[key_RelayCode].Value
                : localLobby.RelayCode.Value;
            localLobby.RelayNGOCode.Value = remoteLobby.Data?.ContainsKey(key_RelayNGOCode) == true
                ? remoteLobby.Data[key_RelayNGOCode].Value
                : localLobby.RelayNGOCode.Value;

            LocalLobby.LobbyData lobbyData = new LocalLobby.LobbyData(localLobby.Data)
            {
                Private = remoteLobby.IsPrivate,
                LobbyName = remoteLobby.Name,
                MaxPlayerCount = remoteLobby.MaxPlayers,
                LastEdit = remoteLobby.LastUpdated.ToFileTimeUtc(),
                LobbyState = remoteLobby.Data?.ContainsKey(key_LobbyState) == true ? (LobbyState)int.Parse(remoteLobby.Data[key_LobbyState].Value) : LobbyState.Lobby,
                LobbyColor = remoteLobby.Data?.ContainsKey(key_LobbyColor) == true ? (LobbyColor)int.Parse(remoteLobby.Data[key_LobbyColor].Value) : LobbyColor.None,
            };

            Dictionary<string, LobbyUser> lobbyUsers = new Dictionary<string, LobbyUser>();
            foreach (var player in remoteLobby.Players)
            {
                // (If we haven't seen this player yet, a new local representation of the player will have already been added by the LocalLobby.)
                LobbyUser incomingData = new LobbyUser
                {
                    IsHost = remoteLobby.HostId.Equals(player.Id),
                    DisplayName = player.Data?.ContainsKey(key_Displayname) == true ? player.Data[key_Displayname].Value : default,
                    Emote = player.Data?.ContainsKey(key_Emote) == true ? (EmoteType)int.Parse(player.Data[key_Emote].Value) : default,
                    UserStatus = player.Data?.ContainsKey(key_Userstatus) == true ? (UserStatus)int.Parse(player.Data[key_Userstatus].Value) : UserStatus.Connecting,
                    ID = player.Id,
                };
                lobbyUsers.Add(incomingData.ID, incomingData);
            }

            //Push all the data at once so we don't call OnChanged for each variable
            localLobby.CopyObserved(lobbyData, lobbyUsers);
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
