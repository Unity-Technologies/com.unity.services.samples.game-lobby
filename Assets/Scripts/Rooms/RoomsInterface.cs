using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Rooms;
using Unity.Services.Rooms.Models;
using Unity.Services.Rooms.Rooms;

namespace LobbyRooms.Rooms
{
    /// <summary>
    /// Does all the interactions with Rooms.
    /// </summary>
    public static class RoomsInterface
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
                try {
                    result = await task; // TODO: We lose call stacks here. Can that be prevented?
                } finally {
                    onComplete?.Invoke(result);
                }
            }
        }

        /// <summary>
        /// Overwrite the base Path on Awake to point the service somewhere else.
        /// </summary>
        public static void SetPath(string path = "https://rooms.cloud.unity3d.com/v1")
        {
            Configuration.BasePath = path;
        }

        private const int k_maxRoomsToShow = 64;

        public static void CreateRoomAsync(string requesterUASId, string roomName, int maxPlayers, Action<Response<Room>> onComplete)
        {
            CreateRoomRequest createRequest = new CreateRoomRequest(new CreateRequest(
                name: roomName,
                player: new Unity.Services.Rooms.Models.Player(requesterUASId),
                maxPlayers: maxPlayers,
                isPrivate: false
            ));
            var task = RoomsService.RoomsApiClient.CreateRoomAsync(createRequest);
            new InProgressRequest<Response<Room>>(task, onComplete);
        }

        public static void DeleteRoomAsync(string roomId, Action<Response> onComplete)
        {
            DeleteRoomRequest deleteRequest = new DeleteRoomRequest(roomId);
            var task = RoomsService.RoomsApiClient.DeleteRoomAsync(deleteRequest);
            new InProgressRequest<Response>(task, onComplete);
        }

        public static void JoinRoomAsync(string requesterUASId, string roomId, string roomCode, Action<Response<Room>> onComplete)
        {
            JoinRoomRequest joinRequest = new JoinRoomRequest(new JoinRequest(
                player: new Unity.Services.Rooms.Models.Player(requesterUASId), // TODO: Oh, we can supply initial data here.
                id: roomId,
                roomCode: roomCode
            ));
            var task = RoomsService.RoomsApiClient.JoinRoomAsync(joinRequest);
            new InProgressRequest<Response<Room>>(task, onComplete);
        }

        public static void LeaveRoomAsync(string requesterUASId, string roomId, Action<Response> onComplete)
        {
            RemovePlayerRequest leaveRequest = new RemovePlayerRequest(roomId, requesterUASId);
            var task = RoomsService.RoomsApiClient.RemovePlayerAsync(leaveRequest);
            new InProgressRequest<Response>(task, onComplete);
        }

        public static void QueryAllRoomsAsync(Action<Response<QueryResponse>> onComplete)
        {
            QueryRoomsRequest queryRequest = new QueryRoomsRequest(new QueryRequest(count: k_maxRoomsToShow));
            var task = RoomsService.RoomsApiClient.QueryRoomsAsync(queryRequest);
            new InProgressRequest<Response<QueryResponse>>(task, onComplete);
        }

        public static void GetRoomAsync(string roomId, Action<Response<Room>> onComplete)
        {
            GetRoomRequest getRequest = new GetRoomRequest(roomId);
            var task = RoomsService.RoomsApiClient.GetRoomAsync(getRequest);
            new InProgressRequest<Response<Room>>(task, onComplete);
        }

        public static void UpdateRoomAsync(string roomId, Dictionary<string, DataObject> data, Action<Response<Room>> onComplete)
        {
            UpdateRoomRequest updateRequest = new UpdateRoomRequest(roomId, new UpdateRequest(
                data: data
            ));
            var task = RoomsService.RoomsApiClient.UpdateRoomAsync(updateRequest);
            new InProgressRequest<Response<Room>>(task, onComplete);
        }

        public static void UpdatePlayerAsync(string roomId, string playerId, Dictionary<string, DataObject> data, Action<Response<Room>> onComplete)
        {
            UpdatePlayerRequest updateRequest = new UpdatePlayerRequest(roomId, playerId, new PlayerUpdateRequest(
                data: data
            ));
            var task = RoomsService.RoomsApiClient.UpdatePlayerAsync(updateRequest);
            new InProgressRequest<Response<Room>>(task, onComplete);
        }
    }
}
