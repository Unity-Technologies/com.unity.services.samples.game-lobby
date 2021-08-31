using LobbyRelaySample.lobby;
using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
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
            Locator.Get.UpdateSlow.Subscribe(UpdateLobby, 0.5f); // Shouldn't need to unsubscribe since this instance won't be replaced. 0.5s is arbitrary; the rate limits are tracked later.
        }

        #region Once connected to a lobby, cache the local lobby object so we don't query for it for every lobby operation.

        // (This assumes that the player will be actively in just one lobby at a time, though they could passively be in more.)
        private string m_currentLobbyId = null;
        private Lobby m_lastKnownLobby;
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
                {
                    m_lastKnownLobby = lobby;
                }
            }
        }

        #endregion

        #region Lobby API calls are rate limited, and some other operations might want an alert when the rate limits have passed.

        // Note that some APIs limit to 1 call per N seconds, while others limit to M calls per N seconds. We'll treat all APIs as though they limited to 1 call per N seconds.
        public enum RequestType
        {
            Query = 0,
            Join,
            QuickJoin
        }

        public RateLimitCooldown GetRateLimit(RequestType type)
        {
            if (type == RequestType.Join)
                return m_rateLimitJoin;
            else if (type == RequestType.QuickJoin)
                return m_rateLimitQuickJoin;
            return m_rateLimitQuery;
        }

        private RateLimitCooldown m_rateLimitQuery = new RateLimitCooldown(1.5f); // Used for both the lobby list UI and the in-lobby updating. In the latter case, updates can be cached.
        private RateLimitCooldown m_rateLimitJoin = new RateLimitCooldown(3f);
        private RateLimitCooldown m_rateLimitQuickJoin = new RateLimitCooldown(10f);

        // TODO: Shift to using this to do rate limiting for all API calls? E.g. the lobby data pushing is on its own loop.

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

            void OnLobbyCreated(Lobby response)
            {
                if (response == null)
                    onFailure?.Invoke();
                else
                    onSuccess?.Invoke(response); // The Create request automatically joins the lobby, so we need not take further action.
            }
        }

        /// <summary>
        /// Attempt to join an existing lobby. Either ID xor code can be null.
        /// </summary>
        public void JoinLobbyAsync(string lobbyId, string lobbyCode, LobbyUser localUser, Action<Lobby> onSuccess, Action onFailure)
        {
            if (!m_rateLimitJoin.CanCall() ||
                (lobbyId == null && lobbyCode == null))
            {
                onFailure?.Invoke();

                // TODO: Emit some failure message.
                return;
            }

            string uasId = AuthenticationService.Instance.PlayerId;
            if (!string.IsNullOrEmpty(lobbyId))
                LobbyAPIInterface.JoinLobbyAsync_ById(uasId, lobbyId, CreateInitialPlayerData(localUser), OnLobbyJoined);
            else
                LobbyAPIInterface.JoinLobbyAsync_ByCode(uasId, lobbyCode, CreateInitialPlayerData(localUser), OnLobbyJoined);

            void OnLobbyJoined(Lobby response)
            {
                if (response == null)
                    onFailure?.Invoke();
                else
                    onSuccess?.Invoke(response);
            }
        }

        public void QuickJoinLobbyAsync(LobbyUser localUser, LobbyColor limitToColor = LobbyColor.None, Action<Lobby> onSuccess = null, Action onFailure = null)
        {
            if (!m_rateLimitQuickJoin.CanCall())
            {
                onFailure?.Invoke();
                m_rateLimitQuery.EnqueuePendingOperation(() => { QuickJoinLobbyAsync(localUser, limitToColor, onSuccess, onFailure); });
                return;
            }

            var filters = LobbyColorToFilters(limitToColor);
            string uasId = AuthenticationService.Instance.PlayerId;
            LobbyAPIInterface.QuickJoinLobbyAsync(uasId, filters, CreateInitialPlayerData(localUser), OnLobbyJoined);

            void OnLobbyJoined(Lobby response)
            {
                if (response == null)
                    onFailure?.Invoke();
                else
                    onSuccess?.Invoke(response);
            }
        }

        /// <summary>
        /// Used for getting the list of all active lobbies, without needing full info for each.
        /// </summary>
        /// <param name="onListRetrieved">If called with null, retrieval was unsuccessful. Else, this will be given a list of contents to display, as pairs of a lobby code and a display string for that lobby.</param>
        public void RetrieveLobbyListAsync(Action<QueryResponse> onListRetrieved, Action<QueryResponse> onError = null, LobbyColor limitToColor = LobbyColor.None)
        {
            if (!m_rateLimitQuery.CanCall())
            {
                onListRetrieved?.Invoke(null);
                m_rateLimitQuery.EnqueuePendingOperation(() => { RetrieveLobbyListAsync(onListRetrieved, onError, limitToColor); });
                return;
            }

            var filters = LobbyColorToFilters(limitToColor);

            LobbyAPIInterface.QueryAllLobbiesAsync(filters, OnLobbyListRetrieved);

            void OnLobbyListRetrieved(QueryResponse response)
            {
                if (response != null)
                    onListRetrieved?.Invoke(response);
                else
                    onError?.Invoke(response);
            }
        }

        private List<QueryFilter> LobbyColorToFilters(LobbyColor limitToColor)
        {
            List<QueryFilter> filters = new List<QueryFilter>();
            if (limitToColor == LobbyColor.Orange)
                filters.Add(new QueryFilter(QueryFilter.FieldOptions.N1, ((int)LobbyColor.Orange).ToString(), QueryFilter.OpOptions.EQ));
            else if (limitToColor == LobbyColor.Green)
                filters.Add(new QueryFilter(QueryFilter.FieldOptions.N1, ((int)LobbyColor.Green).ToString(), QueryFilter.OpOptions.EQ));
            else if (limitToColor == LobbyColor.Blue)
                filters.Add(new QueryFilter(QueryFilter.FieldOptions.N1, ((int)LobbyColor.Blue).ToString(), QueryFilter.OpOptions.EQ));
            return filters;
        }

        /// <param name="onComplete">If no lobby is retrieved, or if this call hits the rate limit, this is given null.</param>
        private void RetrieveLobbyAsync(string lobbyId, Action<Lobby> onComplete)
        {
            if (!m_rateLimitQuery.CanCall())
            {
                onComplete?.Invoke(null);
                return;
            }

            LobbyAPIInterface.GetLobbyAsync(lobbyId, OnGet);

            void OnGet(Lobby response)
            {
                onComplete?.Invoke(response);
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

            void OnLeftLobby()
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
            if (m_rateLimitQuery.IsInCooldown)
            {
                m_rateLimitQuery.EnqueuePendingOperation(caller);
                return false;
            }

            Lobby lobby = m_lastKnownLobby;
            if (lobby == null)
            {
                if (shouldRetryIfLobbyNull)
                    m_rateLimitQuery.EnqueuePendingOperation(caller);
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

        public class RateLimitCooldown : Observed<RateLimitCooldown>
        {
            private float m_timeSinceLastCall = float.MaxValue;
            private readonly float m_cooldownTime;
            private Queue<Action> m_pendingOperations = new Queue<Action>();
            private bool m_isHandlingPending = false; // Just in case a pending operation tries to enqueue itself again.

            public void EnqueuePendingOperation(Action action)
            {
                if (!m_isHandlingPending)
                    m_pendingOperations.Enqueue(action);
            }

            private bool m_isInCooldown = false;

            public bool IsInCooldown
            {
                get => m_isInCooldown;
                private set
                {
                    if (m_isInCooldown != value)
                    {
                        m_isInCooldown = value;
                        OnChanged(this);
                    }
                }
            }

            public RateLimitCooldown(float cooldownTime)
            {
                m_cooldownTime = cooldownTime;
            }

            public bool CanCall()
            {
                if (m_timeSinceLastCall < m_cooldownTime)
                    return false;
                else
                {
                    Locator.Get.UpdateSlow.Subscribe(OnUpdate, m_cooldownTime);
                    m_timeSinceLastCall = 0;
                    IsInCooldown = true;
                    return true;
                }
            }

            private void OnUpdate(float dt)
            {
                m_timeSinceLastCall += dt;
                m_isHandlingPending = false; // (Backup in case a pending operation hit an exception.)
                if (m_timeSinceLastCall >= m_cooldownTime)
                {
                    IsInCooldown = false;
                    if (!m_isInCooldown) // It's possible that by setting IsInCooldown, something called CanCall immediately, in which case we want to stay on UpdateSlow.
                    {
                        Locator.Get.UpdateSlow.Unsubscribe(OnUpdate); // Note that this is after IsInCooldown is set, to prevent an Observer from kicking off CanCall again immediately.
                        m_isHandlingPending = true;
                        while (m_pendingOperations.Count > 0)
                            m_pendingOperations.Dequeue()?.Invoke(); // Note: If this ends up enqueuing many operations, we might need to batch them and/or ensure they don't all execute at once.
                        m_isHandlingPending = false;
                    }
                }
            }

            public override void CopyObserved(RateLimitCooldown oldObserved)
            {
                /* This behavior isn't needed; we're just here for the OnChanged event management. */
            }
        }
    }
}
