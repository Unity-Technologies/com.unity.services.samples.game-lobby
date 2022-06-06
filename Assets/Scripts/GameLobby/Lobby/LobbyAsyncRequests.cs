using LobbyRelaySample.lobby;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace LobbyRelaySample
{
    /// <summary>
    /// An abstraction layer between the direct calls into the Lobby API and the outcomes you actually want. E.g. you can request to get a readable list of
    /// current lobbies and not need to make the query call directly.
    /// </summary>
    public class LobbyAsyncRequests : IDisposable
    {
        // Just doing a singleton since static access is all that's really necessary but we also need to be able to subscribe to the slow update loop.
        private static LobbyAsyncRequests s_instance;
        private const int k_maxLobbiesToShow = 16; // If more are necessary, consider retrieving paginated results or using filters.

        public static LobbyAsyncRequests Instance
        {
            get
            {
                if (s_instance == null)
                    s_instance = new LobbyAsyncRequests();
                return s_instance;
            }
        }


        public Action<Lobby> onLobbyUpdated;

        //Once connected to a lobby, cache the local lobby object so we don't query for it for every lobby operation.
        // (This assumes that the player will be actively in just one lobby at a time, though they could passively be in more.)
        Lobby m_RemoteLobby;


        #region Lobby API calls are rate limited, and some other operations might want an alert when the rate limits have passed.

        // Note that some APIs limit to 1 call per N seconds, while others limit to M calls per N seconds. We'll treat all APIs as though they limited to 1 call per N seconds.
        // Also, this is seralized, so don't reorder the values unless you know what that will affect.
        public enum RequestType
        {
            Query = 0,
            Join,
            QuickJoin,
            Host
        }

        public RateLimitCooldown GetRateLimit(RequestType type)
        {
            if (type == RequestType.Join)
                return m_rateLimitJoin;
            else if (type == RequestType.QuickJoin)
                return m_rateLimitQuickJoin;
            else if (type == RequestType.Host)
                return m_rateLimitHost;
            return m_rateLimitQuery;
        }

        private RateLimitCooldown m_rateLimitQuery = new RateLimitCooldown(1.5f); // Used for both the lobby list UI and the in-lobby updating. In the latter case, updates can be cached.
        private RateLimitCooldown m_rateLimitJoin = new RateLimitCooldown(3f);
        private RateLimitCooldown m_rateLimitQuickJoin = new RateLimitCooldown(10f);
        private RateLimitCooldown m_rateLimitHost = new RateLimitCooldown(3f);

        #endregion

        private static Dictionary<string, PlayerDataObject> CreateInitialPlayerData(LobbyUser player)
        {
            Dictionary<string, PlayerDataObject> data = new Dictionary<string, PlayerDataObject>();
            PlayerDataObject dataObjName = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, player.DisplayName);
            data.Add("DisplayName", dataObjName);
            return data;
        }

        //TODO Back to Polling i Guess


        /// <summary>
        /// Attempt to create a new lobby and then join it.
        /// </summary>
        public async Task<Lobby> CreateLobbyAsync(string lobbyName, int maxPlayers, bool isPrivate, LobbyUser localUser)
        {
            if (m_rateLimitHost.IsInCooldown)
            {
                UnityEngine.Debug.LogWarning("Create Lobby hit the rate limit.");
                return null;
            }

            try
            {
                string uasId = AuthenticationService.Instance.PlayerId;

                CreateLobbyOptions createOptions = new CreateLobbyOptions
                {
                    IsPrivate = isPrivate,
                    Player = new Player(id: uasId, data: CreateInitialPlayerData(localUser))
                };
                var lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createOptions);
#pragma warning disable 4014
                LobbyHeartBeatLoop();
#pragma warning restore 4014

                JoinLobby(lobby);
                return lobby;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Lobby Create failed:\n{ex}");
                return null;
            }
        }


        public async Task<Lobby> GetLobbyAsync(string lobbyId)
        {
            await m_rateLimitQuery.WaitUntilCooldown();

            return await LobbyService.Instance.GetLobbyAsync(lobbyId);
        }

        /// <summary>
        /// Attempt to join an existing lobby. Either ID xor code can be null.
        /// </summary>
        public async Task<Lobby> JoinLobbyAsync(string lobbyId, string lobbyCode, LobbyUser localUser)
        {
            if (m_rateLimitJoin.IsInCooldown ||
                (lobbyId == null && lobbyCode == null))
            {
                return null;
            }

            string uasId = AuthenticationService.Instance.PlayerId;
            Lobby joinedLobby = null;
            var playerData = CreateInitialPlayerData(localUser);
            if (!string.IsNullOrEmpty(lobbyId))
            {
                JoinLobbyByIdOptions joinOptions = new JoinLobbyByIdOptions { Player = new Player(id: uasId, data: playerData) };
                joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, joinOptions);
            }
            else
            {
                JoinLobbyByCodeOptions joinOptions = new JoinLobbyByCodeOptions { Player = new Player(id: uasId, data: playerData) };
                joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinOptions);
            }

            JoinLobby(joinedLobby);
            return joinedLobby;
        }

        /// <summary>
        /// Attempt to join the first lobby among the available lobbies that match the filtered limitToColor.
        /// </summary>
        public async Task<Lobby> QuickJoinLobbyAsync(LobbyUser localUser, LobbyColor limitToColor = LobbyColor.None)
        {
            if (m_rateLimitQuickJoin.IsInCooldown)
            {
                UnityEngine.Debug.LogWarning("Quick Join Lobby hit the rate limit.");
                return null;
            }
            var filters = LobbyColorToFilters(limitToColor);
            string uasId = AuthenticationService.Instance.PlayerId;


            var joinRequest = new QuickJoinLobbyOptions
            {
                Filter = filters,
                Player = new Player(id: uasId, data:  CreateInitialPlayerData(localUser))
            };

            var lobby = await LobbyService.Instance.QuickJoinLobbyAsync(joinRequest);
            JoinLobby(lobby);
            return lobby;

        }

        void JoinLobby(Lobby response)
        {
            m_RemoteLobby = response;
        }

        /// <summary>
        /// Used for getting the list of all active lobbies, without needing full info for each.
        /// </summary>
        /// <param name="onListRetrieved">If called with null, retrieval was unsuccessful. Else, this will be given a list of contents to display, as pairs of a lobby code and a display string for that lobby.</param>
        public async Task<QueryResponse> RetrieveLobbyListAsync(LobbyColor limitToColor = LobbyColor.None)
        {
            await m_rateLimitQuery.WaitUntilCooldown();

            Debug.Log("Retrieving Lobby List");
            var filters = LobbyColorToFilters(limitToColor);

            QueryLobbiesOptions queryOptions = new QueryLobbiesOptions
            {
                Count = k_maxLobbiesToShow,
                Filters = filters
            };
            return await LobbyService.Instance.QueryLobbiesAsync(queryOptions);
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

        /// <summary>
        /// Attempt to leave a lobby, and then delete it if no players remain.
        /// </summary>
        /// <param name="onComplete">Called once the request completes, regardless of success or failure.</param>
        public async Task LeaveLobbyAsync(string lobbyId)
        {
            string uasId = AuthenticationService.Instance.PlayerId;
            await LobbyService.Instance.RemovePlayerAsync(lobbyId, uasId);
            m_RemoteLobby = null;

            // Lobbies will automatically delete the lobby if unoccupied, so we don't need to take further action.
        }

        /// <param name="data">Key-value pairs, which will overwrite any existing data for these keys. Presumed to be available to all lobby members but not publicly.</param>
        public async Task UpdatePlayerDataAsync(Dictionary<string, string> data)
        {

            await m_rateLimitQuery.WaitUntilCooldown();
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

            UpdatePlayerOptions updateOptions = new UpdatePlayerOptions
            {
                Data = dataCurr,
                AllocationId = null,
                ConnectionInfo = null
            };
            await LobbyService.Instance.UpdatePlayerAsync(m_RemoteLobby.Id, playerId, updateOptions);
        }

        /// <summary>
        /// Lobby can be provided info about Relay (or any other remote allocation) so it can add automatic disconnect handling.
        /// </summary>
        public async Task UpdatePlayerRelayInfoAsync(string allocationId, string connectionInfo)
        {

            await m_rateLimitQuery.WaitUntilCooldown();
            await AwaitRemoteLobby();
            string playerId = Locator.Get.Identity.GetSubIdentity(Auth.IIdentityType.Auth).GetContent("id");

            UpdatePlayerOptions updateOptions = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>(),
                AllocationId = allocationId,
                ConnectionInfo = connectionInfo
            };
            await LobbyService.Instance.UpdatePlayerAsync(m_RemoteLobby.Id, playerId, updateOptions);
        }

        /// <param name="data">Key-value pairs, which will overwrite any existing data for these keys. Presumed to be available to all lobby members but not publicly.</param>
        public async Task UpdateLobbyDataAsync(Dictionary<string, string> data)
        {
            await m_rateLimitQuery.WaitUntilCooldown();

            Dictionary<string, DataObject> dataCurr = m_RemoteLobby.Data ?? new Dictionary<string, DataObject>();

            var shouldLock = false;
            foreach (var dataNew in data)
            {
                // Special case: We want to be able to filter on our color data, so we need to supply an arbitrary index to retrieve later. Uses N# for numerics, instead of S# for strings.
                DataObject.IndexOptions index = dataNew.Key == "Color" ? DataObject.IndexOptions.N1 : 0;
                DataObject dataObj = new DataObject(DataObject.VisibilityOptions.Public, dataNew.Value, index); // Public so that when we request the list of lobbies, we can get info about them for filtering.
                if (dataCurr.ContainsKey(dataNew.Key))
                    dataCurr[dataNew.Key] = dataObj;
                else
                    dataCurr.Add(dataNew.Key, dataObj);

                //Special Use: Get the state of the Local lobby so we can lock it from appearing in queries if it's not in the "Lobby" State
                if (dataNew.Key == "State")
                {
                    Enum.TryParse(dataNew.Value, out LobbyState lobbyState);
                    shouldLock = lobbyState != LobbyState.Lobby;
                }
            }
            UpdateLobbyOptions updateOptions = new UpdateLobbyOptions { Data = dataCurr, IsLocked = shouldLock };
            var result = await LobbyService.Instance.UpdateLobbyAsync(m_RemoteLobby.Id, updateOptions);
            if (result != null)
                m_RemoteLobby = result;
        }


        private float m_heartbeatTime = 0;
        private const int k_heartbeatPeriodMS = 8000; // The heartbeat must be rate-limited to 5 calls per 30 seconds. We'll aim for longer in case periods don't align.

        /// <summary>
        /// Lobby requires a periodic ping to detect rooms that are still active, in order to mitigate "zombie" lobbies.
        /// </summary>
        async Task LobbyHeartBeatLoop()
        {
            while (m_RemoteLobby!=null)
            {

#pragma warning disable 4014
                LobbyService.Instance.SendHeartbeatPingAsync(m_RemoteLobby.Id);
#pragma warning restore 4014
                await Task.Delay(k_heartbeatPeriodMS);
            }
        }

        async Task AwaitRemoteLobby()
        {
            while (m_RemoteLobby == null)
                await Task.Delay(100);
        }

        public void Dispose()
        {

        }
        public class RateLimitCooldown
        {
            public Action<bool> onCooldownChange;
            public readonly float m_CooldownSeconds;
            public readonly int m_CoolDownMS;
            Queue<Task> m_TaskQueue = new Queue<Task>();
            Task m_DequeuedTask;

            private bool m_IsInCooldown = false;

            public bool IsInCooldown
            {
                get => m_IsInCooldown;
                private set
                {
                    if (m_IsInCooldown != value)
                    {
                        m_IsInCooldown = value;
                        onCooldownChange?.Invoke(m_IsInCooldown);
                    }
                }
            }

            public RateLimitCooldown(float cooldownSeconds)
            {
                m_CooldownSeconds = cooldownSeconds;
                m_CoolDownMS = Mathf.FloorToInt(m_CooldownSeconds * 1000);
            }


            public async Task WaitUntilCooldown() //TODO YAGNI Handle Multiple commands? Return bool if already waiting?
            {
                //No Queue!
                if (CanCall())
                    return;

                while (m_IsInCooldown)
                {
                    await Task.Delay(100);
                }
            }

            bool CanCall()
            {
                if (!IsInCooldown)
                {
#pragma warning disable 4014
                    CoolDownAsync();
#pragma warning restore 4014
                    return true;
                }
                else return false;

            }

            async Task CoolDownAsync()
            {
                if (m_IsInCooldown)
                    return;
                IsInCooldown = true;
                await Task.Delay(m_CoolDownMS);
                if (m_TaskQueue.Count > 0)
                {
                    m_DequeuedTask = m_TaskQueue.Dequeue();
                    await CoolDownAsync();
                }
                IsInCooldown = false;
            }
        }

    }
}
