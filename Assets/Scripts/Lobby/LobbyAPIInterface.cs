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
            AsyncRequest.DoRequest(task, onComplete);
        }

        public static void DeleteLobbyAsync(string lobbyId, Action onComplete)
        {
            var task = Lobbies.Instance.DeleteLobbyAsync(lobbyId);
            AsyncRequest.DoRequest(task, onComplete);
        }

        public static void JoinLobbyAsync_ByCode(string requesterUASId, string lobbyCode, Dictionary<string, PlayerDataObject> localUserData, Action<Lobby> onComplete)
        {
            JoinLobbyByCodeOptions joinOptions = new JoinLobbyByCodeOptions { Player = new Player(id: requesterUASId, data: localUserData) };
            var task = Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode, joinOptions);
            AsyncRequest.DoRequest(task, onComplete);
        }

        public static void JoinLobbyAsync_ById(string requesterUASId, string lobbyId, Dictionary<string, PlayerDataObject> localUserData, Action<Lobby> onComplete)
        {
            JoinLobbyByIdOptions joinOptions = new JoinLobbyByIdOptions { Player = new Player(id: requesterUASId, data: localUserData) };
            var task = Lobbies.Instance.JoinLobbyByIdAsync(lobbyId, joinOptions);
            AsyncRequest.DoRequest(task, onComplete);
        }

        public static void QuickJoinLobbyAsync(string requesterUASId, List<QueryFilter> filters, Dictionary<string, PlayerDataObject> localUserData, Action<Lobby> onComplete)
        {
            var joinRequest = new QuickJoinLobbyOptions
            {
                Filter = filters,
                Player = new Player(id: requesterUASId, data: localUserData)
            };

            var task = Lobbies.Instance.QuickJoinLobbyAsync(joinRequest);
            AsyncRequest.DoRequest(task, onComplete);
        }

        public static void LeaveLobbyAsync(string requesterUASId, string lobbyId, Action onComplete)
        {
            var task = Lobbies.Instance.RemovePlayerAsync(lobbyId, requesterUASId);
            AsyncRequest.DoRequest(task, onComplete);
        }

        public static void QueryAllLobbiesAsync(List<QueryFilter> filters, Action<QueryResponse> onComplete)
        {
            QueryLobbiesOptions queryOptions = new QueryLobbiesOptions
            {
                Count = k_maxLobbiesToShow,
                Filters = filters
            };
            var task = Lobbies.Instance.QueryLobbiesAsync(queryOptions);
            AsyncRequest.DoRequest(task, onComplete);
        }

        public static void GetLobbyAsync(string lobbyId, Action<Lobby> onComplete)
        {
            var task = Lobbies.Instance.GetLobbyAsync(lobbyId);
            AsyncRequest.DoRequest(task, onComplete);
        }

        public static void UpdateLobbyAsync(string lobbyId, Dictionary<string, DataObject> data, Action<Lobby> onComplete)
        {
            UpdateLobbyOptions updateOptions = new UpdateLobbyOptions { Data = data };
            var task = Lobbies.Instance.UpdateLobbyAsync(lobbyId, updateOptions);
            AsyncRequest.DoRequest(task, onComplete);
        }

        public static void UpdatePlayerAsync(string lobbyId, string playerId, Dictionary<string, PlayerDataObject> data, Action<Lobby> onComplete, string allocationId, string connectionInfo)
        {
            UpdatePlayerOptions updateOptions = new UpdatePlayerOptions
            {
                Data = data,
                AllocationId = allocationId,
                ConnectionInfo = connectionInfo
            };
            var task = Lobbies.Instance.UpdatePlayerAsync(lobbyId, playerId, updateOptions);
            AsyncRequest.DoRequest(task, onComplete);
        }

        public static void HeartbeatPlayerAsync(string lobbyId)
        {
            var task = Lobbies.Instance.SendHeartbeatPingAsync(lobbyId);
            AsyncRequest.DoRequest(task, null);
        }
    }
}
