using System;
using System.Collections.Generic;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

namespace LobbyRelaySample.lobby
{
    /// <summary>
    /// Wrapper for all the interactions with the Lobby API.
    /// </summary>
    public static class LobbyAPIInterface
    {
        private const int k_maxLobbiesToShow = 16; // If more are necessary, consider retrieving paginated results or using filters.

        public static void CreateLobbyAsync(string requesterUASId, string lobbyName, int maxPlayers, bool isPrivate, Dictionary<string, PlayerDataObject> localUserData, Action<Lobby> onComplete)
        {
            CreateLobbyOptions createOptions = new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                Player = new Player(id: requesterUASId, data: localUserData)
            };
            var task = Lobbies.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createOptions);
            AsyncRequestLobby.Instance.DoRequest(task, onComplete);
        }

        public static void DeleteLobbyAsync(string lobbyId, Action onComplete)
        {
            var task = Lobbies.Instance.DeleteLobbyAsync(lobbyId);
            AsyncRequestLobby.Instance.DoRequest(task, onComplete);
        }

        public static void JoinLobbyAsync_ByCode(string requesterUASId, string lobbyCode, Dictionary<string, PlayerDataObject> localUserData, Action<Lobby> onComplete)
        {
            JoinLobbyByCodeOptions joinOptions = new JoinLobbyByCodeOptions { Player = new Player(id: requesterUASId, data: localUserData) };
            var task = Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode, joinOptions);
            AsyncRequestLobby.Instance.DoRequest(task, onComplete);
        }

        public static void JoinLobbyAsync_ById(string requesterUASId, string lobbyId, Dictionary<string, PlayerDataObject> localUserData, Action<Lobby> onComplete)
        {
            JoinLobbyByIdOptions joinOptions = new JoinLobbyByIdOptions { Player = new Player(id: requesterUASId, data: localUserData) };
            var task = Lobbies.Instance.JoinLobbyByIdAsync(lobbyId, joinOptions);
            AsyncRequestLobby.Instance.DoRequest(task, onComplete);
        }

        public static void QuickJoinLobbyAsync(string requesterUASId, List<QueryFilter> filters, Dictionary<string, PlayerDataObject> localUserData, Action<Lobby> onComplete)
        {
            var joinRequest = new QuickJoinLobbyOptions
            {
                Filter = filters,
                Player = new Player(id: requesterUASId, data: localUserData)
            };

            var task = Lobbies.Instance.QuickJoinLobbyAsync(joinRequest);
            AsyncRequestLobby.Instance.DoRequest(task, onComplete);
        }

        public static void LeaveLobbyAsync(string requesterUASId, string lobbyId, Action onComplete)
        {
            var task = Lobbies.Instance.RemovePlayerAsync(lobbyId, requesterUASId);
            AsyncRequestLobby.Instance.DoRequest(task, onComplete);
        }

        public static void QueryAllLobbiesAsync(List<QueryFilter> filters, Action<QueryResponse> onComplete)
        {
            QueryLobbiesOptions queryOptions = new QueryLobbiesOptions
            {
                Count = k_maxLobbiesToShow,
                Filters = filters
            };
            var task = Lobbies.Instance.QueryLobbiesAsync(queryOptions);
            AsyncRequestLobby.Instance.DoRequest(task, onComplete);
        }

        public static void GetLobbyAsync(string lobbyId, Action<Lobby> onComplete)
        {
            var task = Lobbies.Instance.GetLobbyAsync(lobbyId);
            AsyncRequestLobby.Instance.DoRequest(task, onComplete);
        }

        public static void UpdateLobbyAsync(string lobbyId, Dictionary<string, DataObject> data, bool shouldLock, Action<Lobby> onComplete)
        {
            UpdateLobbyOptions updateOptions = new UpdateLobbyOptions { Data = data , IsLocked = shouldLock};
            var task = Lobbies.Instance.UpdateLobbyAsync(lobbyId, updateOptions);
            AsyncRequestLobby.Instance.DoRequest(task, onComplete);
        }

        public static void UpdatePlayerAsync(string lobbyId, string playerId, Dictionary<string, PlayerDataObject> data, Action<Lobby> onComplete, string allocationId)
        {
            UpdatePlayerOptions updateOptions = new UpdatePlayerOptions
            {
                Data = data,
                AllocationId = allocationId
            };
            var task = Lobbies.Instance.UpdatePlayerAsync(lobbyId, playerId, updateOptions);
            AsyncRequestLobby.Instance.DoRequest(task, onComplete);
        }

        public static void HeartbeatPlayerAsync(string lobbyId)
        {
            var task = Lobbies.Instance.SendHeartbeatPingAsync(lobbyId);
            AsyncRequestLobby.Instance.DoRequest(task, null);
        }
    }
}
