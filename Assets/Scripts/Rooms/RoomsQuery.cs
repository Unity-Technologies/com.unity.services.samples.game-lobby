using LobbyRelaySample.Lobby;
using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Rooms;
using Unity.Services.Rooms.Models;

namespace LobbyRelaySample
{
    /// <summary>
    /// An abstraction layer between the direct calls into Rooms and the outcomes you actually want. E.g. you can request to get a readable list of 
    /// current rooms and not need to make the query call directly.
    /// </summary>
    public class RoomsQuery
    {
        // Just doing a singleton since static access is all that's really necessary but we also need to be able to subscribe to the slow update loop.
        private static RoomsQuery s_instance;

        public static RoomsQuery Instance
        {
            get
            {
                if (s_instance == null)
                    s_instance = new RoomsQuery();
                return s_instance;
            }
        }

        public RoomsQuery()
        {
            Locator.Get.UpdateSlow.Subscribe(UpdateRoom); // Shouldn't need to unsubscribe since this instance won't be replaced.
        }

        private static bool IsSuccessful(Response response)
        {
            return response != null && response.Status >= 200 && response.Status < 300; // Uses HTTP status codes, so 2xx is a success.
        }

        #region We want to cache the room object so we don't query for it every time we need to do a different room operation or view current data.
        // (This assumes that the player will be actively in just one room at a time, though they could passively be in more.)
        private Queue<Action> m_pendingOperations = new Queue<Action>();
        private string m_currentRoomId = null;
        private Room m_lastKnownRoom;
        private bool m_isMidRetrieve = false;
        public Room CurrentRoom => m_lastKnownRoom;

        public void BeginTracking(string roomId)
        {
            m_currentRoomId = roomId;
        }

        public void EndTracking()
        {
            m_currentRoomId = null;
        }

        private void UpdateRoom(float unused)
        {
            if (!string.IsNullOrEmpty(m_currentRoomId))
                RetrieveRoomAsync(m_currentRoomId, OnComplete);

            void OnComplete(Room room)
            {
                if (room != null)
                    m_lastKnownRoom = room;
                m_isMidRetrieve = false;
                HandlePendingOperations();
            }
        }

        private void HandlePendingOperations()
        {
            while (m_pendingOperations.Count > 0)
                m_pendingOperations.Dequeue()?.Invoke(); // Note: If this ends up enqueuing a bunch of operations, we might need to batch them and/or ensure they don't all execute at once.
        }

        #endregion

        /// <summary>
        /// Attempt to create a new room and then join it.
        /// </summary>
        public void CreateRoomAsync(string roomName, int maxPlayers, bool isPrivate, Action<Room> onSuccess, Action onFailure)
        {
            string uasId = AuthenticationService.Instance.PlayerId;
            RoomsInterface.CreateRoomAsync(uasId, roomName, maxPlayers, isPrivate, OnRoomCreated);

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

        /// <summary>Attempt to join an existing room. Either ID xor code can be null.</summary>
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

        /// <summary>Used for getting the list of all active rooms, without needing full info for each.</summary>
        /// <param name="onListRetrieved">If called with null, retrieval was unsuccessful. Else, this will be given a list of contents to display, as pairs of a room code and a display string for that room.</param>
        public void RetrieveRoomListAsync(Action<QueryResponse> onListRetrieved, Action<Response<QueryResponse>> onError = null)
        {
            RoomsInterface.QueryAllRoomsAsync(OnRoomListRetrieved);

            void OnRoomListRetrieved(Response<QueryResponse> response)
            {
                if (IsSuccessful(response))
                    onListRetrieved?.Invoke(response?.Result);
                else
                    onError?.Invoke(response);
            }
        }
        /// <param name="onComplete">If no room is retrieved, this is given null.</param>
        private void RetrieveRoomAsync(string roomId, Action<Room> onComplete)
        {
            if (m_isMidRetrieve)
                return; // Not calling onComplete since there's just the one point at which this is called.
            m_isMidRetrieve = true;
            RoomsInterface.GetRoomAsync(roomId, OnGet);

            void OnGet(Response<Room> response)
            {
                m_isMidRetrieve = false;
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

                // TEMP. As of 6/31/21, the Rooms service doesn't automatically delete emptied rooms, though that functionality is expected in the near-term.
                // Until then, we'll do a delete request whenever we leave, and if it's invalid, we'll just get a 403 back.
                RoomsInterface.DeleteRoomAsync(roomId, null);
            }
        }

        /// <param name="data">Key-value pairs, which will overwrite any existing data for these keys. Presumed to be available to all room members but not publicly.</param>
        public void UpdatePlayerDataAsync(Dictionary<string, string> data, Action onComplete)
        {
            if (!ShouldUpdateData(() => { UpdatePlayerDataAsync(data, onComplete); }, onComplete))
                return;

            Room room = m_lastKnownRoom;
            Dictionary<string, PlayerDataObject> dataCurr = new Dictionary<string, PlayerDataObject>();
            foreach (var dataNew in data)
            {
                PlayerDataObject dataObj = new PlayerDataObject(visibility: PlayerDataObject.VisibilityOptions.Member, value: dataNew.Value);
                if (dataCurr.ContainsKey(dataNew.Key))
                    dataCurr[dataNew.Key] = dataObj;
                else
                    dataCurr.Add(dataNew.Key, dataObj);
            }

            RoomsInterface.UpdatePlayerAsync(room.Id, Locator.Get.Identity.GetSubIdentity(Auth.IIdentityType.Auth).GetContent("id"), dataCurr, (r) => { onComplete?.Invoke(); });
        }

        /// <param name="data">Key-value pairs, which will overwrite any existing data for these keys. Presumed to be available to all room members but not publicly.</param>
        public void UpdateRoomDataAsync(Dictionary<string, string> data, Action onComplete)
        {
            if (!ShouldUpdateData(() => { UpdateRoomDataAsync(data, onComplete); }, onComplete))
                return;

            Room room = m_lastKnownRoom;
            Dictionary<string, DataObject> dataCurr = room.Data ?? new Dictionary<string, DataObject>();
            foreach (var dataNew in data)
            {
                DataObject dataObj = new DataObject(visibility: DataObject.VisibilityOptions.Public, value: dataNew.Value); // Public so that when we request the list of lobbies, we can get info about them for filtering.
                if (dataCurr.ContainsKey(dataNew.Key))
                    dataCurr[dataNew.Key] = dataObj;
                else
                    dataCurr.Add(dataNew.Key, dataObj);
            }
            
            RoomsInterface.UpdateRoomAsync(room.Id, dataCurr, (r) => { onComplete?.Invoke(); });
        }

        private bool ShouldUpdateData(Action caller, Action onComplete)
        {
            if (m_isMidRetrieve)
            {   m_pendingOperations.Enqueue(caller);
                return false;
            }
            Room room = m_lastKnownRoom;
            if (room == null)
            {   onComplete?.Invoke();
                return false;
            }
            return true;
        }
    }
}
