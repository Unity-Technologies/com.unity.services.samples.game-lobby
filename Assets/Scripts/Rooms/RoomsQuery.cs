using LobbyRooms.Rooms;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Authentication;
using Unity.Services.Rooms;
using Unity.Services.Rooms.Models;
using Utilities;

namespace LobbyRooms
{
    /// <summary>
    /// An abstraction layer between the direct calls into Rooms and the outcomes you actually want. E.g. you can request to get a readable list of 
    /// current rooms and not need to make the query call directly.
    /// </summary>
    public class RoomsQuery
    {
        // Just doing a singleton since static access is all that's really necessary but we also need to be able to subscribe to the slow update loop.
        private static RoomsQuery s_instance;
        public static RoomsQuery Instance { 
            get {
                if (s_instance == null)
                    s_instance = new RoomsQuery();
                return s_instance;
            }
        }

        private static bool IsSuccessful(Response response)
        {
            return response != null && response.Status >= 200 && response.Status < 300; // Uses HTTP status codes, so 2xx is a success.
        }

        // TODO: Reify async calls so they can be enqueued if calls are made before outstanding async operations complete?

        /// <summary>
        /// Attempt to create a new room and then join it.
        /// </summary>
        public void CreateRoomAsync(string roomName, int maxPlayers, Action<Room> onSuccess, Action onFailure)
        {
            string uasId = AuthenticationService.Instance.PlayerId;
            RoomsInterface.CreateRoomAsync(uasId, roomName, maxPlayers, OnRoomCreated);

            void OnRoomCreated(Response<Room> response)
            {
                if (!IsSuccessful(response))
                    onFailure?.Invoke();
                else
                {
                    var pendingRoom = response.Result;
                    onSuccess?.Invoke(pendingRoom); // The CreateRoom request automatically joins the room, so we need not take further action.
                }
            }
        }

        /// <summary>Attempt to join an existing room. ID xor code can be null.</summary>
        public void JoinRoomAsync(string roomId, string roomCode, Action<Room> onSuccess, Action onFailure)
        {
            string uasId = AuthenticationService.Instance.PlayerId;
            RoomsInterface.JoinRoomAsync(uasId, roomId, roomCode, OnRoomJoined);

            void OnRoomJoined(Response<Room> response)
            {
                if (!IsSuccessful(response))
                    onFailure?.Invoke();
                else
                    onSuccess?.Invoke(response?.Result);
            }
        }

        /// <param name="onListRetrieved">If called with null, retrieval was unsuccessful. Else, this will be given a list of contents to display, as pairs of a room code and a display string for that room.</param>
        public void RetrieveRoomListAsync(Action<QueryResponse> onListRetrieved)
        {
            string uasId = AuthenticationService.Instance.PlayerId;
            RoomsInterface.QueryAllRoomsAsync(OnRoomListRetrieved);

            void OnRoomListRetrieved(Response<QueryResponse> response)
            {
                if (IsSuccessful(response))
                    onListRetrieved?.Invoke(response?.Result);
            }
        }
        /// <param name="onComplete">If no room is retrieved, this is given null.</param>
        public void RetrieveRoomAsync(string roomId, Action<Room> onComplete)
        {
            RoomsInterface.GetRoomAsync(roomId, OnGet);

            void OnGet(Response<Room> response)
            {
                onComplete?.Invoke(response?.Result);
            }
        }

        /// <summary>
        /// Attempt to leave a room, and then delete it if no players remain.
        /// </summary>
        /// <param name="onComplete">Called once the request completes, regardless of success or failure.</param>
        public void LeaveRoomAsync(string roomId, Action onComplete)
        {
            string uasId = AuthenticationService.Instance.PlayerId;
            RoomsInterface.LeaveRoomAsync(uasId, roomId, OnLeftRoom);

            void OnLeftRoom(Response response)
            {
                onComplete?.Invoke();
                // Rooms will automatically delete the room if unoccupied, so we don't need to take further action.
            }
        }

        /// <param name="room">Pass in the room from a RetrieveRoomAsync. (This prevents a 429 Too Many Requests if updating both player and room data, by using one retrieval.)</param>
        /// <param name="data">Key-value pairs, which will overwrite any existing data for these keys. Presumed to be available to all room members but not publicly.</param>
        public void UpdatePlayerDataAsync(Room room, string userId, Dictionary<string, string> data, Action onComplete)
        {
            var existingPlayerData = room.Players.First(p => p.Id.Equals(userId)).Data;
            var dataCurr = existingPlayerData ?? new Dictionary<string, PlayerDataObject>();

            foreach (var dataNew in data)
            {
                var dataObj = new PlayerDataObject(
                    visibility: PlayerDataObject.VisibilityOptions.Member,
                    value: dataNew.Value);

                if (dataCurr.ContainsKey(dataNew.Key))
                    dataCurr[dataNew.Key] = dataObj;
                else
                    dataCurr.Add(dataNew.Key, dataObj);
            }

            RoomsInterface.UpdatePlayerAsync(
                roomId: room.Id,
                playerId: Locator.Get.Identity.GetSubIdentity(Auth.IIdentityType.Auth).GetContent("id"),
                data: dataCurr,
                onComplete: (r) => { onComplete?.Invoke(); });
        }

        /// <param name="room">Pass in the room from a RetrieveRoomAsync. (This prevents a 429 Too Many Requests if updating both player and room data, by using one retrieval.)</param>
        /// <param name="data">Key-value pairs, which will overwrite any existing data for these keys. Presumed to be available to all room members but not publicly.</param>
        public void UpdateRoomDataAsync(Room room, Dictionary<string, string> data, Action onComplete)
        {
            Dictionary<string, DataObject> dataCurr = room.Data ?? new Dictionary<string, DataObject>();

            foreach (var dataNew in data)
            {
                DataObject dataObj = new DataObject(visibility: DataObject.VisibilityOptions.Member, value: dataNew.Value);

                if (dataCurr.ContainsKey(dataNew.Key))
                    dataCurr[dataNew.Key] = dataObj;
                else
                    dataCurr.Add(dataNew.Key, dataObj);
            }

            RoomsInterface.UpdateRoomAsync(
                roomId: room.Id,
                data: dataCurr,
                onComplete: (r) => { onComplete?.Invoke(); });
        }
    }
}
