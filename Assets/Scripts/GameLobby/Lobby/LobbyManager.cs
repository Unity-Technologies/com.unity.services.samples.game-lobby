using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    public class LobbyManager: IDisposable
    {

        //Once connected to a lobby, cache the local lobby object so we don't query for it for every lobby operation.
        // (This assumes that the game will be actively in just one lobby at a time, though they could be in more on the service side.)

        public Lobby CurrentLobby => m_currentLobby;
        const int k_maxLobbiesToShow = 16; // If more are necessary, consider retrieving paginated results or using filters.
        Lobby m_currentLobby;


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

        public RateLimiter GetRateLimit(RequestType type)
        {
            if (type == RequestType.Join)
                return m_JoinCooldown;
            else if (type == RequestType.QuickJoin)
                return m_QuickJoinCooldown;
            else if (type == RequestType.Host)
                return m_CreateCooldown;
            return m_QueryCooldown;
        }

        RateLimiter m_QueryCooldown = new RateLimiter(1f); // Used for both the lobby list UI and the in-lobby updating. In the latter case, updates can be cached.
        RateLimiter m_CreateCooldown = new RateLimiter(3f);
        RateLimiter m_JoinCooldown = new RateLimiter(3f);
        RateLimiter m_QuickJoinCooldown = new RateLimiter(10f);
        RateLimiter m_GetLobbyCooldown = new RateLimiter(1f);
        RateLimiter m_DeleteLobbyCooldown = new RateLimiter(.2f);
        RateLimiter m_UpdateLobbyCooldown = new RateLimiter(.2f);
        RateLimiter m_UpdatePlayerCooldown = new RateLimiter(.2f);
        RateLimiter m_LeaveLobbyOrRemovePlayer = new RateLimiter(.2f);
        RateLimiter m_HeartBeatCooldown = new RateLimiter(6f);


        #endregion

        static Dictionary<string, PlayerDataObject> CreateInitialPlayerData(LobbyUser user)
        {
            Dictionary<string, PlayerDataObject> data = new Dictionary<string, PlayerDataObject>();

            var displayNameObject = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, user.DisplayName);
            var emoteNameObject = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, user.Emote.ToString());

            data.Add("DisplayName", displayNameObject);
            data.Add("Emote", emoteNameObject);
            return data;
        }



        /// <summary>
        /// Attempt to create a new lobby and then join it.
        /// </summary>
        public async Task<Lobby> CreateLobbyAsync(string lobbyName, int maxPlayers, bool isPrivate, LobbyUser localUser)
        {
            if (m_CreateCooldown.IsInCooldown)
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
            await m_GetLobbyCooldown.WaitUntilCooldown();

            return await LobbyService.Instance.GetLobbyAsync(lobbyId);
        }

        /// <summary>
        /// Attempt to join an existing lobby. Either ID xor code can be null.
        /// </summary>
        public async Task<Lobby> JoinLobbyAsync(string lobbyId, string lobbyCode, LobbyUser localUser)
        {
            //Dont want to queue the join action in this case.
            if (m_JoinCooldown.IsInCooldown ||
                (lobbyId == null && lobbyCode == null))
            {
                return null;
            }

            await m_JoinCooldown.WaitUntilCooldown();

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

            return joinedLobby;
        }

        /// <summary>
        /// Attempt to join the first lobby among the available lobbies that match the filtered limitToColor.
        /// </summary>
        public async Task<Lobby> QuickJoinLobbyAsync(LobbyUser localUser, LobbyColor limitToColor = LobbyColor.None)
        {
            //We dont want to queue a quickjoin
            if (m_QuickJoinCooldown.IsInCooldown)
            {
                UnityEngine.Debug.LogWarning("Quick Join Lobby hit the rate limit.");
                return null;
            }

            await m_QuickJoinCooldown.WaitUntilCooldown();
            var filters = LobbyColorToFilters(limitToColor);
            string uasId = AuthenticationService.Instance.PlayerId;


            var joinRequest = new QuickJoinLobbyOptions
            {
                Filter = filters,
                Player = new Player(id: uasId, data:  CreateInitialPlayerData(localUser))
            };

            var lobby = await LobbyService.Instance.QuickJoinLobbyAsync(joinRequest);
            return lobby;
        }


        /// <summary>
        /// Used for getting the list of all active lobbies, without needing full info for each.
        /// </summary>
        /// <param name="onListRetrieved">If called with null, retrieval was unsuccessful. Else, this will be given a list of contents to display, as pairs of a lobby code and a display string for that lobby.</param>
        public async Task<QueryResponse> RetrieveLobbyListAsync(LobbyColor limitToColor = LobbyColor.None)
        {
            await m_QueryCooldown.WaitUntilCooldown();

            var filters = LobbyColorToFilters(limitToColor);

            QueryLobbiesOptions queryOptions = new QueryLobbiesOptions
            {
                Count = k_maxLobbiesToShow,
                Filters = filters
            };
            return await LobbyService.Instance.QueryLobbiesAsync(queryOptions);
        }

        List<QueryFilter> LobbyColorToFilters(LobbyColor limitToColor)
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
            await m_LeaveLobbyOrRemovePlayer.WaitUntilCooldown();

            string playerId = AuthenticationService.Instance.PlayerId;
            await LobbyService.Instance.RemovePlayerAsync(lobbyId, playerId);
        }

        /// <param name="data">Key-value pairs, which will overwrite any existing data for these keys. Presumed to be available to all lobby members but not publicly.</param>
        public async Task UpdatePlayerDataAsync(string lobbyID, Dictionary<string, string> data)
        {
            await m_UpdatePlayerCooldown.WaitUntilCooldown();
            string playerId = AuthenticationService.Instance.PlayerId;
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
            await LobbyService.Instance.UpdatePlayerAsync(lobbyID, playerId, updateOptions);
        }

        /// <summary>
        /// Lobby can be provided info about Relay (or any other remote allocation) so it can add automatic disconnect handling.
        /// </summary>
        public async Task UpdatePlayerRelayInfoAsync(string lobbyID, string allocationId, string connectionInfo)
        {
            await m_UpdatePlayerCooldown.WaitUntilCooldown();
            string playerId = AuthenticationService.Instance.PlayerId;

            UpdatePlayerOptions updateOptions = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>(),
                AllocationId = allocationId,
                ConnectionInfo = connectionInfo
            };
            await LobbyService.Instance.UpdatePlayerAsync(lobbyID, playerId, updateOptions);
        }

        /// <param name="data">Key-value pairs, which will overwrite any existing data for these keys. Presumed to be available to all lobby members but not publicly.</param>
        public async Task<Lobby> UpdateLobbyDataAsync(Lobby remoteLobby, Dictionary<string, string> data)
        {
            await m_UpdateLobbyCooldown.WaitUntilCooldown();

            Dictionary<string, DataObject> dataCurr = remoteLobby.Data ?? new Dictionary<string, DataObject>();

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
            var result = await LobbyService.Instance.UpdateLobbyAsync(remoteLobby.Id, updateOptions);
            return result;
        }

        public async Task SendHeartbeatPingAsync(string remoteLobbyId)
        {
            if (m_HeartBeatCooldown.IsInCooldown)
                return;
            await m_HeartBeatCooldown.WaitUntilCooldown();
            await LobbyService.Instance.SendHeartbeatPingAsync(remoteLobbyId);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public class RateLimiter
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

            public RateLimiter(float cooldownSeconds)
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
                    await Task.Delay(50);
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
