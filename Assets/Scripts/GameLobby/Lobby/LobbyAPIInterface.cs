﻿using System;
using Unity.Services.Lobbies;

namespace LobbyRelaySample.lobby
{
    /// <summary>
    /// Wrapper for all the interactions with the Lobby API.
    /// </summary>
    public static class LobbyAPIInterface
    {
        /* TODO Delete LobbyAPIInterface
        public static void CreateLobbyAsync(string requesterUASId, string lobbyName, int maxPlayers, bool isPrivate, Dictionary<string, PlayerDataObject> localUserData, Action<Lobby> onComplete)
        {
            CreateLobbyOptions createOptions = new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                Player = new Player(id: requesterUASId, data: localUserData)
            };
            var task = LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createOptions);
            AsyncRequestLobby.Instance.DoRequest(task, onComplete);
        }

        public static void DeleteLobbyAsync(string lobbyId, Action onComplete)
        {
            var task = LobbyService.Instance.DeleteLobbyAsync(lobbyId);
            AsyncRequestLobby.Instance.DoRequest(task, onComplete);
        }

        public static void JoinLobbyAsync_ByCode(string requesterUASId, string lobbyCode, Dictionary<string, PlayerDataObject> localUserData, Action<Lobby> onComplete)
        {
            JoinLobbyByCodeOptions joinOptions = new JoinLobbyByCodeOptions { Player = new Player(id: requesterUASId, data: localUserData) };
            var task = LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinOptions);
            AsyncRequestLobby.Instance.DoRequest(task, onComplete);
        }

        public static void JoinLobbyAsync_ById(string requesterUASId, string lobbyId, Dictionary<string, PlayerDataObject> localUserData, Action<Lobby> onComplete)
        {
            JoinLobbyByIdOptions joinOptions = new JoinLobbyByIdOptions { Player = new Player(id: requesterUASId, data: localUserData) };
            var task = LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, joinOptions);
            AsyncRequestLobby.Instance.DoRequest(task, onComplete);
        }

        public static void QuickJoinLobbyAsync(string requesterUASId, List<QueryFilter> filters, Dictionary<string, PlayerDataObject> localUserData, Action<Lobby> onComplete)
        {
            var joinRequest = new QuickJoinLobbyOptions
            {
                Filter = filters,
                Player = new Player(id: requesterUASId, data: localUserData)
            };

            var task = LobbyService.Instance.QuickJoinLobbyAsync(joinRequest);
            AsyncRequestLobby.Instance.DoRequest(task, onComplete);
        }

        public static void LeaveLobbyAsync(string requesterUASId, string lobbyId, Action onComplete)
        {
            var task = LobbyService.Instance.RemovePlayerAsync(lobbyId, requesterUASId);
            AsyncRequestLobby.Instance.DoRequest(task, onComplete);
        }

        /// <summary>
        /// Uupdates custom data to the lobby, for all to see.
        /// </summary>
        public static void UpdateLobbyAsync(string lobbyId, Dictionary<string, DataObject> data, bool shouldLock, Action<Lobby> onComplete)
        {
            UpdateLobbyOptions updateOptions = new UpdateLobbyOptions { Data = data, IsLocked = shouldLock };
            var task = LobbyService.Instance.UpdateLobbyAsync(lobbyId, updateOptions);
            AsyncRequestLobby.Instance.DoRequest(task, onComplete);
        }

        public static void UpdatePlayerAsync(string lobbyId, string playerId, Dictionary<string, PlayerDataObject> data, Action<Lobby> onComplete, string allocationId, string connectionInfo)
        {
            UpdatePlayerOptions updateOptions = new UpdatePlayerOptions
            {
                Data = data,
                AllocationId = allocationId,
                ConnectionInfo = connectionInfo
            };
            var task = LobbyService.Instance.UpdatePlayerAsync(lobbyId, playerId, updateOptions);
            AsyncRequestLobby.Instance.DoRequest(task, onComplete);
        }*/

        public static void SubscribeToLobbyUpdates(string lobbyId, LobbyEventCallbacks lobbyEvent, Action<ILobbyEvents> onLobbySubscribed)
        {
            var task = LobbyService.Instance.SubscribeToLobbyEventsAsync(lobbyId, lobbyEvent);
            AsyncRequestLobby.Instance.DoRequest(task, onLobbySubscribed);
        }

        public static void HeartbeatPlayerAsync(string lobbyId)
        {
            var task = LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            AsyncRequestLobby.Instance.DoRequest(task, null);
        }
    }
}