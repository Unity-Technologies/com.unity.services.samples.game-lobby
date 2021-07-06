using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Rooms;
using Unity.Services.Rooms.Models;
using Unity.Services.Rooms.Rooms;

namespace LobbyRelaySample.Lobby
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

        private const int k_maxLobbiesToShow = 64;

        public static void CreateLobbyAsync(string requesterUASId, string lobbyName, int maxPlayers, bool isPrivate, Action<Response<Room>> onComplete)
        {
            CreateRoomRequest createRequest = new CreateRoomRequest(new CreateRequest(
                name: lobbyName,
                player: new Unity.Services.Rooms.Models.Player(requesterUASId),
                maxPlayers: maxPlayers,
                isPrivate: isPrivate
            ));
            var task = RoomsService.RoomsApiClient.CreateRoomAsync(createRequest);
            new InProgressRequest<Response<Room>>(task, onComplete);
        }

        public static void DeleteLobbyAsync(string lobbyId, Action<Response> onComplete)
        {
            DeleteRoomRequest deleteRequest = new DeleteRoomRequest(lobbyId);
            var task = RoomsService.RoomsApiClient.DeleteRoomAsync(deleteRequest);
            new InProgressRequest<Response>(task, onComplete);
        }

        public static void JoinLobbyAsync(string requesterUASId, string lobbyId, string lobbyCode, Action<Response<Room>> onComplete)
        {
            JoinRoomRequest joinRequest = new JoinRoomRequest(new JoinRequest(
                player: new Unity.Services.Rooms.Models.Player(requesterUASId),
                id: lobbyId,
                roomCode: lobbyCode
            ));
            var task = RoomsService.RoomsApiClient.JoinRoomAsync(joinRequest);
            new InProgressRequest<Response<Room>>(task, onComplete);
        }

        public static void LeaveLobbyAsync(string requesterUASId, string lobbyId, Action<Response> onComplete)
        {
            RemovePlayerRequest leaveRequest = new RemovePlayerRequest(lobbyId, requesterUASId);
            var task = RoomsService.RoomsApiClient.RemovePlayerAsync(leaveRequest);
            new InProgressRequest<Response>(task, onComplete);
        }

        public static void QueryAllLobbiesAsync(Action<Response<QueryResponse>> onComplete)
        {
            QueryRoomsRequest queryRequest = new QueryRoomsRequest(new QueryRequest(count: k_maxLobbiesToShow));
            var task = RoomsService.RoomsApiClient.QueryRoomsAsync(queryRequest);
            new InProgressRequest<Response<QueryResponse>>(task, onComplete);
        }

        public static void GetLobbyAsync(string lobbyId, Action<Response<Room>> onComplete)
        {
            GetRoomRequest getRequest = new GetRoomRequest(lobbyId);
            var task = RoomsService.RoomsApiClient.GetRoomAsync(getRequest);
            new InProgressRequest<Response<Room>>(task, onComplete);
        }

        public static void UpdateLobbyAsync(string lobbyId, Dictionary<string, DataObject> data, Action<Response<Room>> onComplete)
        {
            UpdateRoomRequest updateRequest = new UpdateRoomRequest(lobbyId, new UpdateRequest(
                data: data
            ));
            var task = RoomsService.RoomsApiClient.UpdateRoomAsync(updateRequest);
            new InProgressRequest<Response<Room>>(task, onComplete);
        }

        public static void UpdatePlayerAsync(string lobbyId, string playerId, Dictionary<string, PlayerDataObject> data, Action<Response<Room>> onComplete)
        {
            UpdatePlayerRequest updateRequest = new UpdatePlayerRequest(lobbyId, playerId, new PlayerUpdateRequest(
                data: data
            ));
            var task = RoomsService.RoomsApiClient.UpdatePlayerAsync(updateRequest);
            new InProgressRequest<Response<Room>>(task, onComplete);
        }
    }
}
