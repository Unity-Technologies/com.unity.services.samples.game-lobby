using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace LobbyRelaySample.lobby
{
    /// <summary>
    /// QueryToLocalList the lobby resulting from a request into a LocalLobby for use in the game logic.
    /// </summary>
    public static class LobbyConverters
    {
        public const string key_RelayCode = nameof(LocalLobby.RelayCode);
        public const string key_LobbyState = nameof(LocalLobby.LocalLobbyState);
        public const string key_LobbyColor = nameof(LocalLobby.LocalLobbyColor);
        public const string key_LastEdit = nameof(LocalLobby.LastUpdated);

        public const string key_Displayname = nameof(LocalPlayer.DisplayName);
        public const string key_Userstatus = nameof(LocalPlayer.UserStatus);
        public const string key_Emote = nameof(LocalPlayer.Emote);

        public static Dictionary<string, string> LocalToRemoteLobbyData(LocalLobby lobby)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add(key_RelayCode, lobby.RelayCode.Value);
            data.Add(key_LobbyState, ((int)lobby.LocalLobbyState.Value).ToString());
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
            data.Add(key_Userstatus, ((int)user.UserStatus.Value).ToString());
            data.Add(key_Emote, ((int)user.Emote.Value).ToString());
            return data;
        }

        /// <summary>
        /// Create a new LocalLobby from the content of a retrieved lobby. Its data can be copied into an existing LocalLobby for use.
        /// </summary>
        public static void RemoteToLocal(Lobby remoteLobby, LocalLobby localLobby)
        {
            if (remoteLobby == null)
            {
                Debug.LogError("Remote lobby is null, cannot convert.");
                return;
            }

            if (localLobby == null)
            {
                Debug.LogError("Local Lobby is null, cannot convert");
                return;
            }

            localLobby.LobbyID.Value = remoteLobby.Id;
            localLobby.HostID.Value = remoteLobby.HostId;
            localLobby.LobbyName.Value = remoteLobby.Name;
            localLobby.LobbyCode.Value = remoteLobby.LobbyCode;
            localLobby.Private.Value = remoteLobby.IsPrivate;
            localLobby.AvailableSlots.Value = remoteLobby.AvailableSlots;
            localLobby.MaxPlayerCount.Value = remoteLobby.MaxPlayers;
            localLobby.LastUpdated.Value = remoteLobby.LastUpdated.ToFileTimeUtc();

            //Custom Lobby Data Conversions
            localLobby.RelayCode.Value = remoteLobby.Data?.ContainsKey(key_RelayCode) == true
                ? remoteLobby.Data[key_RelayCode].Value
                : localLobby.RelayCode.Value;
            localLobby.LocalLobbyState.Value = remoteLobby.Data?.ContainsKey(key_LobbyState) == true
                ? (LobbyState)int.Parse(remoteLobby.Data[key_LobbyState].Value)
                : LobbyState.Lobby;
            localLobby.LocalLobbyColor.Value = remoteLobby.Data?.ContainsKey(key_LobbyColor) == true
                ? (LobbyColor)int.Parse(remoteLobby.Data[key_LobbyColor].Value)
                : LobbyColor.None;

            //Custom User Data Conversions
            List<string> remotePlayerIDs = new List<string>();
            int index = 0;
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
                    : EmoteType.None;
                var userStatus = player.Data?.ContainsKey(key_Userstatus) == true
                    ? (PlayerStatus)int.Parse(player.Data[key_Userstatus].Value)
                    : PlayerStatus.Lobby;

                LocalPlayer localPlayer = localLobby.GetLocalPlayer(index);

                if (localPlayer == null)
                {
                    localPlayer = new LocalPlayer(id, index, isHost, displayName, emote, userStatus);
                    localLobby.AddPlayer(index, localPlayer);
                }
                else
                {
                    localPlayer.ID.Value = id;
                    localPlayer.Index.Value = index;
                    localPlayer.IsHost.Value = isHost;
                    localPlayer.DisplayName.Value = displayName;
                    localPlayer.Emote.Value = emote;
                    localPlayer.UserStatus.Value = userStatus;
                }
                index++;
            }

        }

        /// <summary>
        /// Create a list of new LocalLobbies from the result of a lobby list query.
        /// </summary>
        public static List<LocalLobby> QueryToLocalList(QueryResponse response)
        {
            List<LocalLobby> returnList = new List<LocalLobby>();
            if (response == null)
                return returnList;

            foreach (var lobby in response.Results)
                returnList.Add(RemoteToNewLocal(lobby));
            return returnList;
        }

        //This might be heavy handed,
        static LocalLobby RemoteToNewLocal(Lobby lobby)
        {
            LocalLobby localLobby = new LocalLobby();
            RemoteToLocal(lobby, localLobby);
            return localLobby;
        }
    }
}