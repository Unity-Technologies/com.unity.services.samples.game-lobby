using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LobbyRelaySample.lobby;
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
    ///
    /// Manages one Lobby at a time, Only entry points to a lobby with ID is via JoinAsync, CreateAsync, and QuickJoinAsync
    public class LobbyManager
    {
        //Once connected to a lobby, cache the local lobby object so we don't query for it for every lobby operation.
        // (This assumes that the game will be actively in just one lobby at a time, though they could be in more on the service side.)

        public event Action OnKicked;
        Lobby m_CurrentLobby;
        LocalLobby m_CurrentLocalLobby;
        LobbyEventCallbacks m_LobbyEventCallbacks;
        const int
            k_maxLobbiesToShow = 16; // If more are necessary, consider retrieving paginated results or using filters.

        Task m_HeartBeatTask;

        public LobbyManager()
        {
            m_LobbyEventCallbacks = new LobbyEventCallbacks();
        }

        #region Rate Limiting

        public enum RequestType
        {
            Query = 0,
            Join,
            QuickJoin,
            Host
        }

        public bool InLobby()
        {
            if (m_CurrentLobby == null)
            {
                Debug.LogWarning("LobbyManager not currently in a lobby. Did you CreateLobbyAsync or JoinLobbyAsync?");
                return false;
            }

            return true;
        }

        public ServiceRateLimiter GetRateLimit(RequestType type)
        {
            if (type == RequestType.Join)
                return m_JoinCooldown;
            else if (type == RequestType.QuickJoin)
                return m_QuickJoinCooldown;
            else if (type == RequestType.Host)
                return m_CreateCooldown;
            return m_QueryCooldown;
        }

        // Rate Limits are posted here: https://docs.unity.com/lobby/rate-limits.html

        ServiceRateLimiter m_QueryCooldown = new ServiceRateLimiter(1, 1f);
        ServiceRateLimiter m_CreateCooldown = new ServiceRateLimiter(2, 6f);
        ServiceRateLimiter m_JoinCooldown = new ServiceRateLimiter(2, 6f);
        ServiceRateLimiter m_QuickJoinCooldown = new ServiceRateLimiter(1, 10f);
        ServiceRateLimiter m_GetLobbyCooldown = new ServiceRateLimiter(1, 1f);
        ServiceRateLimiter m_DeleteLobbyCooldown = new ServiceRateLimiter(2, 1f);
        ServiceRateLimiter m_UpdateLobbyCooldown = new ServiceRateLimiter(5, 5f);
        ServiceRateLimiter m_UpdatePlayerCooldown = new ServiceRateLimiter(5, 5f);
        ServiceRateLimiter m_LeaveLobbyOrRemovePlayer = new ServiceRateLimiter(5, 1);
        ServiceRateLimiter m_HeartBeatCooldown = new ServiceRateLimiter(5, 30);

        #endregion

        #region LobbyWrappers

        Dictionary<string, PlayerDataObject> CreateInitialPlayerData(LocalPlayer user)
        {
            Dictionary<string, PlayerDataObject> data = new Dictionary<string, PlayerDataObject>();

            var displayNameObject =
                new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, user.DisplayName.Value);
            data.Add("DisplayName", displayNameObject);
            return data;
        }

        public async Task<Lobby> CreateLobbyAsync(string lobbyName, int maxPlayers, bool isPrivate,
            LocalPlayer localUser)
        {
            if (m_CreateCooldown.IsCoolingDown)
            {
                Debug.LogWarning("Create Lobby hit the rate limit.");
                return null;
            }

            await m_CreateCooldown.QueueUntilCooldown();

            try
            {
                string uasId = AuthenticationService.Instance.PlayerId;

                CreateLobbyOptions createOptions = new CreateLobbyOptions
                {
                    IsPrivate = isPrivate,
                    Player = new Player(id: uasId, data: CreateInitialPlayerData(localUser))
                };
                m_CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createOptions);
                StartHeartBeat();

                return m_CurrentLobby;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Lobby Create failed:\n{ex}");
                return null;
            }
        }

        public async Task<Lobby> JoinLobbyAsync(string lobbyId, string lobbyCode, LocalPlayer localUser)
        {
            if (m_JoinCooldown.IsCoolingDown ||
                (lobbyId == null && lobbyCode == null))
            {
                return null;
            }

            await m_JoinCooldown.QueueUntilCooldown();

            string uasId = AuthenticationService.Instance.PlayerId;
            var playerData = CreateInitialPlayerData(localUser);

            if (!string.IsNullOrEmpty(lobbyId))
            {
                JoinLobbyByIdOptions joinOptions = new JoinLobbyByIdOptions
                    { Player = new Player(id: uasId, data: playerData) };
                m_CurrentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, joinOptions);
            }
            else
            {
                JoinLobbyByCodeOptions joinOptions = new JoinLobbyByCodeOptions
                    { Player = new Player(id: uasId, data: playerData) };
                m_CurrentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinOptions);
            }

            return m_CurrentLobby;
        }

        public async Task<Lobby> QuickJoinLobbyAsync(LocalPlayer localUser, LobbyColor limitToColor = LobbyColor.None)
        {
            //We dont want to queue a quickjoin
            if (m_QuickJoinCooldown.IsCoolingDown)
            {
                Debug.LogWarning("Quick Join Lobby hit the rate limit.");
                return null;
            }

            await m_QuickJoinCooldown.QueueUntilCooldown();
            var filters = LobbyColorToFilters(limitToColor);
            string uasId = AuthenticationService.Instance.PlayerId;

            var joinRequest = new QuickJoinLobbyOptions
            {
                Filter = filters,
                Player = new Player(id: uasId, data: CreateInitialPlayerData(localUser))
            };

            return m_CurrentLobby = await LobbyService.Instance.QuickJoinLobbyAsync(joinRequest);
        }

        public async Task<QueryResponse> RetrieveLobbyListAsync(LobbyColor limitToColor = LobbyColor.None)
        {
            var filters = LobbyColorToFilters(limitToColor);

            if (m_QueryCooldown.TaskQueued)
                return null;
            await m_QueryCooldown.QueueUntilCooldown();

            QueryLobbiesOptions queryOptions = new QueryLobbiesOptions
            {
                Count = k_maxLobbiesToShow,
                Filters = filters
            };

            return await LobbyService.Instance.QueryLobbiesAsync(queryOptions);
        }

        public async Task<Lobby> GetLobbyAsync(string lobbyId = null)
        {
            if (m_GetLobbyCooldown.TaskQueued)
                return null;
            await m_GetLobbyCooldown.QueueUntilCooldown();

            if (!InLobby())
                return null;

            lobbyId ??= m_CurrentLobby.Id;
            return await LobbyService.Instance.GetLobbyAsync(lobbyId);
        }

        public async Task LeaveLobbyAsync()
        {
            if (m_LeaveLobbyOrRemovePlayer.IsCoolingDown)
                return;
            await m_LeaveLobbyOrRemovePlayer.QueueUntilCooldown();
            if (!InLobby())
                return;
            await LobbyService.Instance.RemovePlayerAsync(m_CurrentLobby.Id, AuthenticationService.Instance.PlayerId);
        }

        public async Task UpdatePlayerDataAsync(Dictionary<string, string> data)
        {
            string playerId = AuthenticationService.Instance.PlayerId;

            Dictionary<string, PlayerDataObject> dataCurr = new Dictionary<string, PlayerDataObject>();
            foreach (var dataNew in data)
            {
                PlayerDataObject dataObj = new PlayerDataObject(
                    visibility: PlayerDataObject.VisibilityOptions.Member,
                    value: dataNew.Value);

                if (dataCurr.ContainsKey(dataNew.Key))
                    dataCurr[dataNew.Key] = dataObj;
                else
                    dataCurr.Add(dataNew.Key, dataObj);
            }

            if (m_UpdatePlayerCooldown.TaskQueued)
                return;
            await m_UpdatePlayerCooldown.QueueUntilCooldown();

            if (!InLobby())
                return;
            UpdatePlayerOptions updateOptions = new UpdatePlayerOptions
            {
                Data = dataCurr,
                AllocationId = null,
                ConnectionInfo = null
            };
            Debug.Log($"SENDING : {dataCurr[LobbyConverters.key_Displayname].Value}'s Data" +
                $" TO : \n {m_CurrentLocalLobby}");
            await LobbyService.Instance.UpdatePlayerAsync(m_CurrentLobby.Id, playerId, updateOptions);
        }

        public async Task UpdatePlayerRelayInfoAsync(string lobbyID, string allocationId, string connectionInfo)
        {
            if (!InLobby())
                return;

            string playerId = AuthenticationService.Instance.PlayerId;

            if (m_UpdatePlayerCooldown.TaskQueued)
                return;
            await m_UpdatePlayerCooldown.QueueUntilCooldown();

            UpdatePlayerOptions updateOptions = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>(),
                AllocationId = allocationId,
                ConnectionInfo = connectionInfo
            };
            await LobbyService.Instance.UpdatePlayerAsync(lobbyID, playerId, updateOptions);
        }

        public async Task UpdateLobbyDataAsync(Dictionary<string, string> data)
        {
            Dictionary<string, DataObject> dataCurr = m_CurrentLobby.Data ?? new Dictionary<string, DataObject>();

            var shouldLock = false;
            foreach (var dataNew in data)
            {
                // Special case: We want to be able to filter on our color data, so we need to supply an arbitrary index to retrieve later. Uses N# for numerics, instead of S# for strings.
                DataObject.IndexOptions index = dataNew.Key == "LocalLobbyColor" ? DataObject.IndexOptions.N1 : 0;
                DataObject dataObj = new DataObject(DataObject.VisibilityOptions.Public, dataNew.Value,
                    index); // Public so that when we request the list of lobbies, we can get info about them for filtering.
                if (dataCurr.ContainsKey(dataNew.Key))
                    dataCurr[dataNew.Key] = dataObj;
                else
                    dataCurr.Add(dataNew.Key, dataObj);

                //Special Use: Get the state of the Local lobby so we can lock it from appearing in queries if it's not in the "Lobby" LocalLobbyState
                if (dataNew.Key == LobbyConverters.key_LobbyState)
                {
                    Enum.TryParse(dataNew.Value, out LobbyState lobbyState);
                    shouldLock = lobbyState != LobbyState.Lobby;
                }
            }

            //We can still update the latest data to send to the service, but we will not send multiple UpdateLobbySyncCalls
            if (m_UpdateLobbyCooldown.TaskQueued)
                return;
            await m_UpdateLobbyCooldown.QueueUntilCooldown();

            if (!InLobby())
                return;

            UpdateLobbyOptions updateOptions = new UpdateLobbyOptions { Data = dataCurr, IsLocked = shouldLock };
            Debug.Log($"Updating Data in {m_CurrentLocalLobby}");
            await LobbyService.Instance.UpdateLobbyAsync(m_CurrentLobby.Id, updateOptions);
        }

        public async Task DeleteLobbyAsync()
        {
            if (!InLobby())
                return;
            await m_DeleteLobbyCooldown.QueueUntilCooldown();

            await LobbyService.Instance.DeleteLobbyAsync(m_CurrentLobby.Id);
        }

        #endregion

        #region LobbyBindings

        public async Task BindLocalLobbyToRemote(string lobbyID, LocalLobby localLobby)
        {
            m_CurrentLocalLobby = localLobby;
            m_LobbyEventCallbacks.LobbyChanged += ProcessLobbyChanges;
            m_LobbyEventCallbacks.LobbyEventConnectionStateChanged += OnStateChanged;
            m_LobbyEventCallbacks.KickedFromLobby += OnRemovedFromLobby;

            await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobbyID, m_LobbyEventCallbacks);
        }

        void OnStateChanged(LobbyEventConnectionState newState)
        {
            Debug.Log($"State Changed! {newState}");
        }

        void OnRemovedFromLobby()
        {
            m_CurrentLobby = null;
            m_CurrentLocalLobby = null;
            OnKicked?.Invoke();

            m_LobbyEventCallbacks.LobbyChanged -= ProcessLobbyChanges;
            m_LobbyEventCallbacks.KickedFromLobby -= OnRemovedFromLobby;
            m_LobbyEventCallbacks.LobbyEventConnectionStateChanged -= OnStateChanged;
        }

        void ProcessLobbyChanges(ILobbyChanges changes)
        {
            if (changes.LobbyDeleted)
            {
                OnRemovedFromLobby();
                return;
            }

            LobbyFieldChanges(changes, m_CurrentLocalLobby);

            CustomLobbyChanged(changes.Data, m_CurrentLocalLobby);

            PlayersJoined(changes.PlayerJoined, m_CurrentLocalLobby);

            PlayersLeft(changes.PlayerLeft, m_CurrentLocalLobby);

            PlayerDataChanged(changes.PlayerData, m_CurrentLocalLobby);
        }

        void LobbyFieldChanges(ILobbyChanges changes, LocalLobby localLobby)
        {
            if (changes.Name.Changed)
                localLobby.LobbyName.Value = changes.Name.Value;
            if (changes.HostId.Changed)
                localLobby.HostID.Value = changes.HostId.Value;
            if (changes.IsPrivate.Changed)
                localLobby.Private.Value = changes.IsPrivate.Value;
            if (changes.IsLocked.Changed)
                localLobby.Locked.Value = changes.IsLocked.Value;
            if (changes.AvailableSlots.Changed)
                localLobby.AvailableSlots.Value = changes.AvailableSlots.Value;
            if (changes.MaxPlayers.Changed)
                localLobby.MaxPlayerCount.Value = changes.MaxPlayers.Value;
            if (changes.LastUpdated.Changed)
                localLobby.LastUpdated.Value = changes.LastUpdated.Value.ToFileTimeUtc();
        }

        void CustomLobbyChanged(
            ChangedOrRemovedLobbyValue<Dictionary<string, ChangedOrRemovedLobbyValue<DataObject>>> lobbyChanged,
            LocalLobby localLobby)
        {
            if (!lobbyChanged.Changed)
                return;

            var lobbyChanges = lobbyChanged.Value;
            foreach (var change in lobbyChanges)
            {
                var changedValue = change.Value;
                var changedKey = change.Key;

                if (changedValue.Removed)
                {
                    if (changedKey == LobbyConverters.key_RelayCode)
                        localLobby.RelayCode.Value = "";
                }

                if (changedValue.Changed)
                {
                    ParseCustomLobbyData(changedKey, changedValue.Value);
                }
            }

            void ParseCustomLobbyData(string changedKey, DataObject playerDataObject)
            {
                if (changedKey == LobbyConverters.key_RelayCode)
                    localLobby.RelayCode.Value = playerDataObject.Value;

                if (changedKey == LobbyConverters.key_LobbyState)
                    localLobby.LocalLobbyState.Value = (LobbyState)int.Parse(playerDataObject.Value);

                if (changedKey == LobbyConverters.key_LobbyColor)
                    localLobby.LocalLobbyColor.Value = (LobbyColor)int.Parse(playerDataObject.Value);
            }
        }

        void PlayersJoined(ChangedLobbyValue<List<LobbyPlayerJoined>> playersJoinedChanged, LocalLobby localLobby)
        {
            if (!playersJoinedChanged.Changed)
                return;

            foreach (var playerChanges in playersJoinedChanged.Value)
            {
                Player joinedPlayer = playerChanges.Player;

                var id = joinedPlayer.Id;
                var index = playerChanges.PlayerIndex;
                var isHost = localLobby.HostID.Value == id;

                var newPlayer = new LocalPlayer(id, index, isHost);

                foreach (var dataEntry in joinedPlayer.Data)
                {
                    var dataObject = dataEntry.Value;
                    ParseCustomPlayerData(newPlayer, dataEntry.Key, dataObject.Value);
                }

                localLobby.AddPlayer(index, newPlayer);
            }
        }

        void PlayersLeft(ChangedLobbyValue<List<int>> playersLeftChanged, LocalLobby localLobby)
        {
            if (!playersLeftChanged.Changed)
                return;
            foreach (var leftPlayerIndex in playersLeftChanged.Value)
            {
                Debug.Log($"Player {leftPlayerIndex} Left");

                localLobby.RemovePlayer(leftPlayerIndex);
            }
        }

        void PlayerDataChanged(ChangedLobbyValue<Dictionary<int, LobbyPlayerChanges>> playerDataChanged,
            LocalLobby localLobby)
        {
            if (!playerDataChanged.Changed)
                return;

            foreach (var lobbyPlayerChanges in playerDataChanged.Value)
            {
                var playerIndex = lobbyPlayerChanges.Key;
                var localPlayer = localLobby.GetLocalPlayer(playerIndex);
                if (localPlayer == null)
                    continue;

                var playerChanges = lobbyPlayerChanges.Value;
                if (playerChanges.ConnectionInfoChanged.Changed)
                {
                    var connectionInfo = playerChanges.ConnectionInfoChanged.Value;
                    Debug.Log(
                        $"ConnectionInfo for player {playerIndex} changed to {connectionInfo}");
                }

                if (playerChanges.LastUpdatedChanged.Changed) { }

                //There are changes on the Player
                if (playerChanges.ChangedData.Changed)
                {
                    foreach (var playerChange in playerChanges.ChangedData.Value)
                    {
                        var changedValue = playerChange.Value;

                        //There are changes on some of the changes in the player list of changes

                        if (changedValue.Changed)
                        {
                            if (changedValue.Removed)
                            {
                                Debug.LogWarning("This Sample does not remove Player Values currently.");
                                continue;
                            }

                            var playerDataObject = changedValue.Value;

                            ParseCustomPlayerData(localPlayer, playerChange.Key, playerDataObject.Value);
                        }
                    }
                }
            }
        }

        void ParseCustomPlayerData(LocalPlayer player, string dataKey, string playerDataValue)
        {
            Debug.Log($"RECIEVING : {player.DisplayName.Value}\nDATA : {dataKey} - {playerDataValue}");

            if (dataKey == LobbyConverters.key_Emote)
                player.Emote.Value = (EmoteType)int.Parse(playerDataValue);
            else if (dataKey == LobbyConverters.key_Userstatus)
                player.UserStatus.Value = (PlayerStatus)int.Parse(playerDataValue);
            else if (dataKey == LobbyConverters.key_Displayname)
                player.DisplayName.Value = playerDataValue;
        }

        #endregion

        #region HeartBeat

        List<QueryFilter> LobbyColorToFilters(LobbyColor limitToColor)
        {
            List<QueryFilter> filters = new List<QueryFilter>();
            if (limitToColor == LobbyColor.Orange)
                filters.Add(new QueryFilter(QueryFilter.FieldOptions.N1, ((int)LobbyColor.Orange).ToString(),
                    QueryFilter.OpOptions.EQ));
            else if (limitToColor == LobbyColor.Green)
                filters.Add(new QueryFilter(QueryFilter.FieldOptions.N1, ((int)LobbyColor.Green).ToString(),
                    QueryFilter.OpOptions.EQ));
            else if (limitToColor == LobbyColor.Blue)
                filters.Add(new QueryFilter(QueryFilter.FieldOptions.N1, ((int)LobbyColor.Blue).ToString(),
                    QueryFilter.OpOptions.EQ));
            return filters;
        }

//Since the LobbyManager maintains the "connection" to the lobby, we will continue to heartbeat until host leaves.
        async Task SendHeartbeatPingAsync()
        {
            if (!InLobby())
                return;
            if (m_HeartBeatCooldown.IsCoolingDown)
                return;
            await m_HeartBeatCooldown.QueueUntilCooldown();

            await LobbyService.Instance.SendHeartbeatPingAsync(m_CurrentLobby.Id);
        }

        void StartHeartBeat()
        {
#pragma warning disable 4014
            m_HeartBeatTask = HeartBeatLoop();
#pragma warning restore 4014
        }

        async Task HeartBeatLoop()
        {
            while (m_CurrentLobby != null)
            {
                await SendHeartbeatPingAsync();
                await Task.Delay(8000);
            }
        }

        #endregion
    }

//Manages the Amount of times you can hit a service call.
//Adds a buffer to account for ping times.
//Will Queue the latest overflow task for when the cooldown ends.
//Created to mimic the way rate limits are implemented Here:  https://docs.unity.com/lobby/rate-limits.html

    #region Cooldowns

    public class ServiceRateLimiter
    {
        public Action<bool> onCooldownChange;
        public readonly int coolDownMS;
        public bool TaskQueued { get; private set; } = false;

        readonly int m_ServiceCallTimes;
        bool m_CoolingDown = false;
        int m_TaskCounter;

        //(If you're still getting rate limit errors, try increasing the pingBuffer)
        public ServiceRateLimiter(int callTimes, float coolDown, int pingBuffer = 100)
        {
            m_ServiceCallTimes = callTimes;
            m_TaskCounter = m_ServiceCallTimes;
            coolDownMS =
                Mathf.CeilToInt(coolDown * 1000) +
                pingBuffer;
        }

        public async Task QueueUntilCooldown()
        {
            if (!m_CoolingDown)
            {
#pragma warning disable 4014
                ParallelCooldownAsync();
#pragma warning restore 4014
            }

            m_TaskCounter--;

            if (m_TaskCounter > 0)
            {
                return;
            }

            if (!TaskQueued)
                TaskQueued = true;
            else
                return;

            while (m_CoolingDown)
            {
                await Task.Delay(10);
            }
        }

        async Task ParallelCooldownAsync()
        {
            IsCoolingDown = true;
            await Task.Delay(coolDownMS);
            IsCoolingDown = false;
            TaskQueued = false;
            m_TaskCounter = m_ServiceCallTimes;
        }

        public bool IsCoolingDown
        {
            get => m_CoolingDown;
            private set
            {
                if (m_CoolingDown != value)
                {
                    m_CoolingDown = value;
                    onCooldownChange?.Invoke(m_CoolingDown);
                }
            }
        }
    }

    #endregion
}