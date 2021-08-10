using LobbyRelaySample.lobby;
using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

namespace LobbyRelaySample
{
    /// <summary>
    /// An abstraction layer between the direct calls into the Lobby API and the outcomes you actually want. E.g. you can request to get a readable list of 
    /// current lobbies and not need to make the query call directly.
    /// </summary>
    public class LobbyAsyncRequests
    {
        // Just doing a singleton since static access is all that's really necessary but we also need to be able to subscribe to the slow update loop.
        private static LobbyAsyncRequests s_instance;

        public static LobbyAsyncRequests Instance
        {
            get
            {
                if (s_instance == null)
                    s_instance = new LobbyAsyncRequests();
                return s_instance;
            }
        }

        public LobbyAsyncRequests()
        {
            Locator.Get.UpdateSlow.Subscribe(UpdateLobby, 1.5f); // Shouldn't need to unsubscribe since this instance won't be replaced.
        }

        private static bool IsSuccessful(Response response)
        {
            return response != null && response.Status >= 200 && response.Status < 300; // Uses HTTP status codes, so 2xx is a success.
        }

        #region Once connected to a lobby, cache the local lobby object so we don't query for it for every lobby operation.
        // (This assumes that the player will be actively in just one lobby at a time, though they could passively be in more.)
        private Queue<Action> m_pendingOperations = new Queue<Action>();
        private string m_currentLobbyId = null;
        private Lobby m_lastKnownLobby;
        private bool m_isMidRetrieve = false;
        public Lobby CurrentLobby => m_lastKnownLobby;

        public void BeginTracking(string lobbyId)
        {
            m_currentLobbyId = lobbyId;
        }

        public void EndTracking()
        {
            m_currentLobbyId = null;
            m_lastKnownLobby = null;
            m_heartbeatTime = 0;
        }

        private void UpdateLobby(float unused)
        {
            if (!string.IsNullOrEmpty(m_currentLobbyId))
                RetrieveLobbyAsync(m_currentLobbyId, OnComplete);

            void OnComplete(Lobby lobby)
            {
                if (lobby != null)
                    m_lastKnownLobby = lobby;
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

        private static Dictionary<string, PlayerDataObject> CreateInitialPlayerData(LobbyUser player)
        {
            Dictionary<string, PlayerDataObject> data = new Dictionary<string, PlayerDataObject>();
            PlayerDataObject dataObjName = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, player.DisplayName);
            data.Add("DisplayName", dataObjName);
            return data;
        }

        /// <summary>
        /// Attempt to create a new lobby and then join it.
        /// </summary>
        public void CreateLobbyAsync(string lobbyName, int maxPlayers, bool isPrivate, LobbyUser localUser, Action<Lobby> onSuccess, Action onFailure)
        {
            string uasId = AuthenticationService.Instance.PlayerId;
            LobbyAPIInterface.CreateLobbyAsync(uasId, lobbyName, maxPlayers, isPrivate, CreateInitialPlayerData(localUser), OnLobbyCreated);

            void OnLobbyCreated(Response<Lobby> response)
            {
                if (!IsSuccessful(response))
                    onFailure?.Invoke();
                else
                {
                    var pendingLobby = response.Result;
                    onSuccess?.Invoke(pendingLobby); // The Create request automatically joins the lobby, so we need not take further action.
                }
            }
        }

        /// <summary>
        /// Attempt to join an existing lobby. Either ID xor code can be null.
        /// </summary>
        public void JoinLobbyAsync(string lobbyId, string lobbyCode, LobbyUser localUser, Action<Lobby> onSuccess, Action onFailure)
        {
            string uasId = AuthenticationService.Instance.PlayerId;
            if (!string.IsNullOrEmpty(lobbyId))
                LobbyAPIInterface.JoinLobbyAsync_ById(uasId, lobbyId, CreateInitialPlayerData(localUser), OnLobbyJoined);
            else
                LobbyAPIInterface.JoinLobbyAsync_ByCode(uasId, lobbyCode, CreateInitialPlayerData(localUser), OnLobbyJoined);

            void OnLobbyJoined(Response<Lobby> response)
            {
                if (!IsSuccessful(response))
                    onFailure?.Invoke();
                else
                    onSuccess?.Invoke(response?.Result);
            }
        }

        /// <summary>
        /// Used for getting the list of all active lobbies, without needing full info for each.
        /// </summary>
        /// <param name="onListRetrieved">If called with null, retrieval was unsuccessful. Else, this will be given a list of contents to display, as pairs of a lobby code and a display string for that lobby.</param>
        public void RetrieveLobbyListAsync(Action<QueryResponse> onListRetrieved, Action<Response<QueryResponse>> onError = null, LobbyColor limitToColor = LobbyColor.None)
        {
            List<QueryFilter> filters = new List<QueryFilter>();
            if (limitToColor == LobbyColor.Orange)
                filters.Add(new QueryFilter(QueryFilter.FieldOptions.N1, ((int)LobbyColor.Orange).ToString(), QueryFilter.OpOptions.EQ));
            else if (limitToColor == LobbyColor.Green)
                filters.Add(new QueryFilter(QueryFilter.FieldOptions.N1, ((int)LobbyColor.Green).ToString(), QueryFilter.OpOptions.EQ));
            else if (limitToColor == LobbyColor.Blue)
                filters.Add(new QueryFilter(QueryFilter.FieldOptions.N1, ((int)LobbyColor.Blue).ToString(), QueryFilter.OpOptions.EQ));

            LobbyAPIInterface.QueryAllLobbiesAsync(filters, OnLobbyListRetrieved);

            void OnLobbyListRetrieved(Response<QueryResponse> response)
            {
                if (IsSuccessful(response))
                    onListRetrieved?.Invoke(response?.Result);
                else
                    onError?.Invoke(response);
            }
        }
        /// <param name="onComplete">If no lobby is retrieved, this is given null.</param>
        private void RetrieveLobbyAsync(string lobbyId, Action<Lobby> onComplete)
        {
            if (m_isMidRetrieve)
                return; // Not calling onComplete since there's just the one point at which this is called.
            m_isMidRetrieve = true;
            LobbyAPIInterface.GetLobbyAsync(lobbyId, OnGet);

            void OnGet(Response<Lobby> response)
            {
                m_isMidRetrieve = false;
                onComplete?.Invoke(response?.Result);
            }
        }

        /// <summary>
        /// Attempt to leave a lobby, and then delete it if no players remain.
        /// </summary>
        /// <param name="onComplete">Called once the request completes, regardless of success or failure.</param>
        public void LeaveLobbyAsync(string lobbyId, Action onComplete)
        {
            string uasId = AuthenticationService.Instance.PlayerId;
            LobbyAPIInterface.LeaveLobbyAsync(uasId, lobbyId, OnLeftLobby);

            void OnLeftLobby(Response response)
            {
                onComplete?.Invoke();
                // Lobbies will automatically delete the lobby if unoccupied, so we don't need to take further action.
            }
        }

        /// <param name="data">Key-value pairs, which will overwrite any existing data for these keys. Presumed to be available to all lobby members but not publicly.</param>
        public void UpdatePlayerDataAsync(Dictionary<string, string> data, Action onComplete)
        {
            if (!ShouldUpdateData(() => { UpdatePlayerDataAsync(data, onComplete); }, onComplete, false))
                return;

            string playerId = Locator.Get.Identity.GetSubIdentity(Auth.IIdentityType.Auth).GetContent("id");
            Dictionary<string, PlayerDataObject> dataCurr = new Dictionary<string, PlayerDataObject>();
            foreach (var dataNew in data)
            {
                PlayerDataObject dataObj = new PlayerDataObject(visibility: PlayerDataObject.VisibilityOptions.Member, value: dataNew.Value);
                if (dataCurr.ContainsKey(dataNew.Key))
                    dataCurr[dataNew.Key] = dataObj;
                else
                    dataCurr.Add(dataNew.Key, dataObj);
            }

            LobbyAPIInterface.UpdatePlayerAsync(m_lastKnownLobby.Id, playerId, dataCurr, (r) => { onComplete?.Invoke(); }, null, null);
        }

        /// <summary>
        /// Lobby can be provided info about Relay (or any other remote allocation) so it can add automatic disconnect handling.
        /// </summary>
        public void UpdatePlayerRelayInfoAsync(string allocationId, string connectionInfo, Action onComplete)
        {
            if (!ShouldUpdateData(() => { UpdatePlayerRelayInfoAsync(allocationId, connectionInfo, onComplete); }, onComplete, true)) // Do retry here since the RelayUtpSetup that called this might be destroyed right after this.
                return;
            string playerId = Locator.Get.Identity.GetSubIdentity(Auth.IIdentityType.Auth).GetContent("id");
            LobbyAPIInterface.UpdatePlayerAsync(m_lastKnownLobby.Id, playerId, new Dictionary<string, PlayerDataObject>(), (r) => { onComplete?.Invoke(); }, allocationId, connectionInfo);
        }

        /// <param name="data">Key-value pairs, which will overwrite any existing data for these keys. Presumed to be available to all lobby members but not publicly.</param>
        public void UpdateLobbyDataAsync(Dictionary<string, string> data, Action onComplete)
        {
            if (!ShouldUpdateData(() => { UpdateLobbyDataAsync(data, onComplete); }, onComplete, false))
                return;

            Lobby lobby = m_lastKnownLobby;
            Dictionary<string, DataObject> dataCurr = lobby.Data ?? new Dictionary<string, DataObject>();
            foreach (var dataNew in data)
            {
                // Special case: We want to be able to filter on our color data, so we need to supply an arbitrary index to retrieve later. Uses N# for numerics, instead of S# for strings.
                DataObject.IndexOptions index = dataNew.Key == "Color" ? DataObject.IndexOptions.N1 : 0;
                DataObject dataObj = new DataObject(DataObject.VisibilityOptions.Public, dataNew.Value, index); // Public so that when we request the list of lobbies, we can get info about them for filtering.
                if (dataCurr.ContainsKey(dataNew.Key))
                    dataCurr[dataNew.Key] = dataObj;
                else
                    dataCurr.Add(dataNew.Key, dataObj);
            }
            
            LobbyAPIInterface.UpdateLobbyAsync(lobby.Id, dataCurr, (r) => { onComplete?.Invoke(); });
        }

        /// <summary>
        /// If we are in the middle of another operation, hold onto any pending ones until after that.
        /// If we aren't in a lobby yet, leave it to the caller to decide what to do, since some callers might need to retry and others might not.
        /// </summary>
        private bool ShouldUpdateData(Action caller, Action onComplete, bool shouldRetryIfLobbyNull)
        {
            if (m_isMidRetrieve)
            {   m_pendingOperations.Enqueue(caller);
                return false;
            }
            Lobby lobby = m_lastKnownLobby;
            if (lobby == null)
            {
                if (shouldRetryIfLobbyNull)
                    m_pendingOperations.Enqueue(caller);
                onComplete?.Invoke();
                return false;
            }
            return true;
        }

        private float m_heartbeatTime = 0;
        private const float k_heartbeatPeriod = 8; // The heartbeat must be rate-limited to 5 calls per 30 seconds. We'll aim for longer in case periods don't align.
        /// <summary>
        /// Lobby requires a periodic ping to detect rooms that are still active, in order to mitigate "zombie" lobbies.
        /// </summary>
        public void DoLobbyHeartbeat(float dt)
        {
            m_heartbeatTime += dt;
            if (m_heartbeatTime > k_heartbeatPeriod)
            { 
                m_heartbeatTime -= k_heartbeatPeriod;
                LobbyAPIInterface.HeartbeatPlayerAsync(m_lastKnownLobby.Id);
            }
        }
    }
}
