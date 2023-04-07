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
    ///
    /// Manages one Lobby at a time, Only entry points to a lobby with ID is via JoinAsync, CreateAsync, and QuickJoinAsync
    public class LobbyManager : IDisposable
    {
        const string key_RelayCode = nameof(LocalLobby.RelayCode);
        const string key_LobbyState = nameof(LocalLobby.LocalLobbyState);
        const string key_LobbyColor = nameof(LocalLobby.LocalLobbyColor);

        const string key_Displayname = nameof(LocalPlayer.DisplayName);
        const string key_Userstatus = nameof(LocalPlayer.UserStatus);
        const string key_Emote = nameof(LocalPlayer.Emote);

        //Once connected to a lobby, cache the local lobby object so we don't query for it for every lobby operation.
        // (This assumes that the game will be actively in just one lobby at a time, though they could be in more on the service side.)

        public Lobby CurrentLobby => m_CurrentLobby;
        Lobby m_CurrentLobby;
        LobbyEventCallbacks m_LobbyEventCallbacks = new LobbyEventCallbacks();
        const int
            k_maxLobbiesToShow = 16; // If more are necessary, consider retrieving paginated results or using filters.

        Task m_HeartBeatTask;

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

        Dictionary<string, PlayerDataObject> CreateInitialPlayerData(LocalPlayer user)
        {
            Dictionary<string, PlayerDataObject> data = new Dictionary<string, PlayerDataObject>();

            var displayNameObject =
                new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, user.DisplayName.Value);
            data.Add("DisplayName", displayNameObject);
            return data;
        }

        public async Task<Lobby> CreateLobbyAsync(string lobbyName, int maxPlayers, bool isPrivate,
            LocalPlayer localUser, string password)
        {
            if (m_CreateCooldown.IsCoolingDown)
            {
                Debug.LogWarning("Create Lobby hit the rate limit.");
                return null;
            }

            await m_CreateCooldown.QueueUntilCooldown();

            string uasId = AuthenticationService.Instance.PlayerId;

            CreateLobbyOptions createOptions = new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                Player = new Player(id: uasId, data: CreateInitialPlayerData(localUser)),
                Password = password
            };
            m_CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createOptions);
            StartHeartBeat();

            return m_CurrentLobby;
        }

        public async Task<Lobby> JoinLobbyAsync(string lobbyId, string lobbyCode, LocalPlayer localUser,
            string password = null)
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
                    { Player = new Player(id: uasId, data: playerData), Password = password };
                m_CurrentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, joinOptions);
            }
            else
            {
                JoinLobbyByCodeOptions joinOptions = new JoinLobbyByCodeOptions
                    { Player = new Player(id: uasId, data: playerData), Password = password };
                m_CurrentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinOptions);
            }

            return m_CurrentLobby;
        }

        public async Task<Lobby> QuickJoinLobbyAsync(LocalPlayer localUser, LobbyColor limitToColor = LobbyColor.None)
        {
            //We dont want to queue a quickjoin
            if (m_QuickJoinCooldown.IsCoolingDown)
            {
                UnityEngine.Debug.LogWarning("Quick Join Lobby hit the rate limit.");
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

        public async Task BindLocalLobbyToRemote(string lobbyID, LocalLobby localLobby)
        {
            m_LobbyEventCallbacks.LobbyDeleted += async () =>
            {
                await LeaveLobbyAsync();
            };

            m_LobbyEventCallbacks.DataChanged += changes =>
            {
                foreach (var change in changes)
                {
                    var changedValue = change.Value;
                    var changedKey = change.Key;

                    if (changedKey == key_RelayCode)
                        localLobby.RelayCode.Value = changedValue.Value.Value;

                    if (changedKey == key_LobbyState)
                        localLobby.LocalLobbyState.Value = (LobbyState)int.Parse(changedValue.Value.Value);

                    if (changedKey == key_LobbyColor)
                        localLobby.LocalLobbyColor.Value = (LobbyColor)int.Parse(changedValue.Value.Value);
                }
            };

            m_LobbyEventCallbacks.DataAdded += changes =>
            {
                foreach (var change in changes)
                {
                    var changedValue = change.Value;
                    var changedKey = change.Key;

                    if (changedKey == key_RelayCode)
                        localLobby.RelayCode.Value = changedValue.Value.Value;

                    if (changedKey == key_LobbyState)
                        localLobby.LocalLobbyState.Value = (LobbyState)int.Parse(changedValue.Value.Value);

                    if (changedKey == key_LobbyColor)
                        localLobby.LocalLobbyColor.Value = (LobbyColor)int.Parse(changedValue.Value.Value);
                }
            };

            m_LobbyEventCallbacks.DataRemoved += changes =>
            {
                foreach (var change in changes)
                {
                    var changedKey = change.Key;
                    if (changedKey == key_RelayCode)
                        localLobby.RelayCode.Value = "";
                }
            };

            m_LobbyEventCallbacks.PlayerLeft += players =>
            {
                foreach (var leftPlayerIndex in players)
                {
                    localLobby.RemovePlayer(leftPlayerIndex);
                }
            };

            m_LobbyEventCallbacks.PlayerJoined += players =>
            {
                foreach (var playerChanges in players)
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
            };

            m_LobbyEventCallbacks.PlayerDataChanged += changes =>
            {
                foreach (var lobbyPlayerChanges in changes)
                {
                    var playerIndex = lobbyPlayerChanges.Key;
                    var localPlayer = localLobby.GetLocalPlayer(playerIndex);
                    if (localPlayer == null)
                        continue;
                    var playerChanges = lobbyPlayerChanges.Value;

                    //There are changes on the Player
                    foreach (var playerChange in playerChanges)
                    {
                        var changedValue = playerChange.Value;

                        //There are changes on some of the changes in the player list of changes
                        var playerDataObject = changedValue.Value;
                        ParseCustomPlayerData(localPlayer, playerChange.Key, playerDataObject.Value);
                    }
                }
            };

            m_LobbyEventCallbacks.PlayerDataAdded += changes =>
            {
                foreach (var lobbyPlayerChanges in changes)
                {
                    var playerIndex = lobbyPlayerChanges.Key;
                    var localPlayer = localLobby.GetLocalPlayer(playerIndex);
                    if (localPlayer == null)
                        continue;
                    var playerChanges = lobbyPlayerChanges.Value;

                    //There are changes on the Player
                    foreach (var playerChange in playerChanges)
                    {
                        var changedValue = playerChange.Value;

                        //There are changes on some of the changes in the player list of changes
                        var playerDataObject = changedValue.Value;
                        ParseCustomPlayerData(localPlayer, playerChange.Key, playerDataObject.Value);
                    }
                }
            };

            m_LobbyEventCallbacks.PlayerDataRemoved += changes =>
            {
                foreach (var lobbyPlayerChanges in changes)
                {
                    var playerIndex = lobbyPlayerChanges.Key;
                    var localPlayer = localLobby.GetLocalPlayer(playerIndex);
                    if (localPlayer == null)
                        continue;
                    var playerChanges = lobbyPlayerChanges.Value;

                    //There are changes on the Player
                    if (playerChanges == null)
                        continue;

                    foreach (var playerChange in playerChanges.Values)
                    {
                        //There are changes on some of the changes in the player list of changes
                        Debug.LogWarning("This Sample does not remove Player Values currently.");
                    }
                }
            };

            m_LobbyEventCallbacks.LobbyChanged += async changes =>
            {
                //Lobby Fields
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

                //Custom Lobby Fields

                if (changes.PlayerData.Changed)
                    PlayerDataChanged();

                void PlayerDataChanged()
                {
                    foreach (var lobbyPlayerChanges in changes.PlayerData.Value)
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
                    }
                }
            };

            m_LobbyEventCallbacks.LobbyEventConnectionStateChanged += lobbyEventConnectionState =>
            {
                Debug.Log($"Lobby ConnectionState Changed to {lobbyEventConnectionState}");
            };

            m_LobbyEventCallbacks.KickedFromLobby += () =>
            {
                Debug.Log("Left Lobby");
                Dispose();
            };
            await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobbyID, m_LobbyEventCallbacks);
        }

        void ParseCustomPlayerData(LocalPlayer player, string dataKey, string playerDataValue)
        {
            if (dataKey == key_Emote)
                player.Emote.Value = (EmoteType)int.Parse(playerDataValue);
            else if (dataKey == key_Userstatus)
                player.UserStatus.Value = (PlayerStatus)int.Parse(playerDataValue);
            else if (dataKey == key_Displayname)
                player.DisplayName.Value = playerDataValue;
        }

        public async Task<Lobby> GetLobbyAsync(string lobbyId = null)
        {
            if (!InLobby())
                return null;
            await m_GetLobbyCooldown.QueueUntilCooldown();
            lobbyId ??= m_CurrentLobby.Id;
            return m_CurrentLobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);
        }

        public async Task LeaveLobbyAsync()
        {
            await m_LeaveLobbyOrRemovePlayer.QueueUntilCooldown();
            if (!InLobby())
                return;
            string playerId = AuthenticationService.Instance.PlayerId;

            await LobbyService.Instance.RemovePlayerAsync(m_CurrentLobby.Id, playerId);
            Dispose();
        }

        public async Task UpdatePlayerDataAsync(Dictionary<string, string> data)
        {
            if (!InLobby())
                return;

            string playerId = AuthenticationService.Instance.PlayerId;
            Dictionary<string, PlayerDataObject> dataCurr = new Dictionary<string, PlayerDataObject>();
            foreach (var dataNew in data)
            {
                PlayerDataObject dataObj = new PlayerDataObject(visibility: PlayerDataObject.VisibilityOptions.Member,
                    value: dataNew.Value);
                if (dataCurr.ContainsKey(dataNew.Key))
                    dataCurr[dataNew.Key] = dataObj;
                else
                    dataCurr.Add(dataNew.Key, dataObj);
            }

            if (m_UpdatePlayerCooldown.TaskQueued)
                return;
            await m_UpdatePlayerCooldown.QueueUntilCooldown();

            UpdatePlayerOptions updateOptions = new UpdatePlayerOptions
            {
                Data = dataCurr,
                AllocationId = null,
                ConnectionInfo = null
            };
            m_CurrentLobby = await LobbyService.Instance.UpdatePlayerAsync(m_CurrentLobby.Id, playerId, updateOptions);
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
            m_CurrentLobby = await LobbyService.Instance.UpdatePlayerAsync(lobbyID, playerId, updateOptions);
        }

        public async Task UpdateLobbyDataAsync(Dictionary<string, string> data)
        {
            if (!InLobby())
                return;

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
                if (dataNew.Key == "LocalLobbyState")
                {
                    Enum.TryParse(dataNew.Value, out LobbyState lobbyState);
                    shouldLock = lobbyState != LobbyState.Lobby;
                }
            }

            //We can still update the latest data to send to the service, but we will not send multiple UpdateLobbySyncCalls
            if (m_UpdateLobbyCooldown.TaskQueued)
                return;
            await m_UpdateLobbyCooldown.QueueUntilCooldown();

            UpdateLobbyOptions updateOptions = new UpdateLobbyOptions { Data = dataCurr, IsLocked = shouldLock };
            m_CurrentLobby = await LobbyService.Instance.UpdateLobbyAsync(m_CurrentLobby.Id, updateOptions);
        }

        public async Task DeleteLobbyAsync()
        {
            if (!InLobby())
                return;
            await m_DeleteLobbyCooldown.QueueUntilCooldown();

            await LobbyService.Instance.DeleteLobbyAsync(m_CurrentLobby.Id);
        }

        public void Dispose()
        {
            m_CurrentLobby = null;
            m_LobbyEventCallbacks = new LobbyEventCallbacks();
        }

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
}