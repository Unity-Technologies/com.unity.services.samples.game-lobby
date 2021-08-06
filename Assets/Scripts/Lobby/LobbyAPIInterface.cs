using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

namespace LobbyRelaySample.lobby
{
    /// <summary>
    /// Does all the interactions with the Lobby API.
    /// </summary>
    public static class LobbyAPIInterface
    {
        private class InProgressRequest<T>
        {
            public InProgressRequest(Task<T> task, Action<T> onComplete)
            {
                DoRequest(task, onComplete);
            }

            private async void DoRequest(Task<T> task, Action<T> onComplete)
            {
                T result = default;
                string currentTrace = System.Environment.StackTrace;
                try {
                    result = await task;
                } catch (Exception e) {
                    Exception eFull = new Exception($"Call stack before async call:\n{currentTrace}\n", e);
                    throw eFull;
                } finally {
                    onComplete?.Invoke(result);
                }
            }
        }

        private const int k_maxLobbiesToShow = 16; // If more are necessary, consider retrieving paginated results or using filters.

        public static void CreateLobbyAsync(string requesterUASId, string lobbyName, int maxPlayers, bool isPrivate, Dictionary<string, PlayerDataObject> localUserData, Action<Response<Lobby>> onComplete)
        {
            CreateLobbyRequest createRequest = new CreateLobbyRequest(new CreateRequest(
                name: lobbyName,
                player: new Player(id: requesterUASId, data: localUserData),
                maxPlayers: maxPlayers,
                isPrivate: isPrivate
            ));
            var task = LobbyService.LobbyApiClient.CreateLobbyAsync(createRequest);
            new InProgressRequest<Response<Lobby>>(task, onComplete);
        }

        public static void DeleteLobbyAsync(string lobbyId, Action<Response> onComplete)
        {
            DeleteLobbyRequest deleteRequest = new DeleteLobbyRequest(lobbyId);
            var task = LobbyService.LobbyApiClient.DeleteLobbyAsync(deleteRequest);
            new InProgressRequest<Response>(task, onComplete);
        }

        public static void JoinLobbyAsync_ByCode(string requesterUASId, string lobbyCode, Dictionary<string, PlayerDataObject> localUserData, Action<Response<Lobby>> onComplete)
        {
            JoinLobbyByCodeRequest joinRequest = new JoinLobbyByCodeRequest(new JoinByCodeRequest(lobbyCode, new Player(id: requesterUASId, data: localUserData)));
            var task = LobbyService.LobbyApiClient.JoinLobbyByCodeAsync(joinRequest);
            new InProgressRequest<Response<Lobby>>(task, onComplete);
        }

        public static void JoinLobbyAsync_ById(string requesterUASId, string lobbyId, Dictionary<string, PlayerDataObject> localUserData, Action<Response<Lobby>> onComplete)
        {
            JoinLobbyByIdRequest joinRequest = new JoinLobbyByIdRequest(lobbyId, new Player(id: requesterUASId, data: localUserData));
            var task = LobbyService.LobbyApiClient.JoinLobbyByIdAsync(joinRequest);
            new InProgressRequest<Response<Lobby>>(task, onComplete);
        }

        public static void LeaveLobbyAsync(string requesterUASId, string lobbyId, Action<Response> onComplete)
        {
            RemovePlayerRequest leaveRequest = new RemovePlayerRequest(lobbyId, requesterUASId);
            var task = LobbyService.LobbyApiClient.RemovePlayerAsync(leaveRequest);
            new InProgressRequest<Response>(task, onComplete);
        }

        public static void QueryAllLobbiesAsync(List<QueryFilter> filters, Action<Response<QueryResponse>> onComplete)
        {
            QueryLobbiesRequest queryRequest = new QueryLobbiesRequest(new QueryRequest(count: k_maxLobbiesToShow, filter: filters));
            var task = LobbyService.LobbyApiClient.QueryLobbiesAsync(queryRequest);
            new InProgressRequest<Response<QueryResponse>>(task, onComplete);
        }

        public static void GetLobbyAsync(string lobbyId, Action<Response<Lobby>> onComplete)
        {
            GetLobbyRequest getRequest = new GetLobbyRequest(lobbyId);
            var task = LobbyService.LobbyApiClient.GetLobbyAsync(getRequest);
            new InProgressRequest<Response<Lobby>>(task, onComplete);
        }

        public static void UpdateLobbyAsync(string lobbyId, Dictionary<string, DataObject> data, Action<Response<Lobby>> onComplete)
        {
            UpdateLobbyRequest updateRequest = new UpdateLobbyRequest(lobbyId, new UpdateRequest(
                data: data
            ));
            var task = LobbyService.LobbyApiClient.UpdateLobbyAsync(updateRequest);
            new InProgressRequest<Response<Lobby>>(task, onComplete);
        }

        public static void UpdatePlayerAsync(string lobbyId, string playerId, Dictionary<string, PlayerDataObject> data, Action<Response<Lobby>> onComplete)
        {
            UpdatePlayerRequest updateRequest = new UpdatePlayerRequest(lobbyId, playerId, new PlayerUpdateRequest(
                data: data
            ));
            var task = LobbyService.LobbyApiClient.UpdatePlayerAsync(updateRequest);
            new InProgressRequest<Response<Lobby>>(task, onComplete);
        }

        public static void HeartbeatPlayerAsync(string lobbyId)
        {
            HeartbeatRequest request = new HeartbeatRequest(lobbyId);
            var task = LobbyService.LobbyApiClient.HeartbeatAsync(request);
            new InProgressRequest<Response>(task, null);
        }
    }
}
