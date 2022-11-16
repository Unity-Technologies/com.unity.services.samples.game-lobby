using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LobbyRelaySample.ngo;
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
        const string key_RelayNGOCode = nameof(LocalLobby.RelayNGOCode);
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
                Debug.LogError("LobbyManager not currently in a lobby. Did you CreateLobbyAsync or JoinLobbyAsync?");
                return false;
            }

            return true;
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

        // Rate Limits are posted here: https://docs.unity.com/lobby/rate-limits.html

        RateLimiter m_QueryCooldown = new RateLimiter(1f);
        RateLimiter m_CreateCooldown = new RateLimiter(3f);
        RateLimiter m_JoinCooldown = new RateLimiter(3f);
        RateLimiter m_QuickJoinCooldown = new RateLimiter(10f);
        RateLimiter m_GetLobbyCooldown = new RateLimiter(1f);
        RateLimiter m_DeleteLobbyCooldown = new RateLimiter(.2f);
        RateLimiter m_UpdateLobbyCooldown = new RateLimiter(.3f);
        RateLimiter m_UpdatePlayerCooldown = new RateLimiter(.3f);
        RateLimiter m_LeaveLobbyOrRemovePlayer = new RateLimiter(.3f);
        RateLimiter m_HeartBeatCooldown = new RateLimiter(6f);

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
            LocalPlayer localUser)
        {
            if (m_CreateCooldown.IsInCooldown)
            {
                Debug.LogWarning("Create Lobby hit the rate limit.");
                return null;
            }

            await m_CreateCooldown.WaitUntilCooldown();

            Debug.Log("Lobby - Creating");

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
            if (m_JoinCooldown.IsInCooldown ||
                (lobbyId == null && lobbyCode == null))
            {
                return null;
            }

            await m_JoinCooldown.WaitUntilCooldown();
            Debug.Log($"{localUser.DisplayName}({localUser.ID}) Joining  Lobby- {lobbyId} with {lobbyCode}");

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
            if (m_QuickJoinCooldown.IsInCooldown)
            {
                UnityEngine.Debug.LogWarning("Quick Join Lobby hit the rate limit.");
                return null;
            }

            await m_QuickJoinCooldown.WaitUntilCooldown();
            Debug.Log("Lobby - Quick Joining.");
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
            await m_QueryCooldown.WaitUntilCooldown();

            Debug.Log("Lobby - Retrieving List.");

            var filters = LobbyColorToFilters(limitToColor);

            QueryLobbiesOptions queryOptions = new QueryLobbiesOptions
            {
                Count = k_maxLobbiesToShow,
                Filters = filters
            };
            return await LobbyService.Instance.QueryLobbiesAsync(queryOptions);
        }


        public async Task SubscribeToLocalLobbyChanges(string lobbyID, LocalLobby localLobby)
        {
            m_LobbyEventCallbacks.LobbyChanged += async changes =>
            {
                if (changes.LobbyDeleted)
                {
                    await LeaveLobbyAsync();
                    return;
                }

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
                if (changes.Data.Changed)
                    LobbyChanged();

                if (changes.PlayerJoined.Changed)
                    PlayersJoined();

                if (changes.PlayerLeft.Changed)
                    PlayersLeft();

                if (changes.PlayerData.Changed)
                    PlayerDataChanged();

                void LobbyChanged()
                {
                    foreach (var change in changes.Data.Value)
                    {
                        var changedValue = change.Value;
                        if (changedValue.Changed)
                        {
                            var changedKey = change.Key;
                            ParseRemoteLobbyData(changedKey, changedValue.Value);
                        }
                    }

                    void ParseRemoteLobbyData(string changedKey, DataObject playerDataObject)
                    {
                        if (changedKey == key_RelayCode)
                            localLobby.RelayCode.Value = playerDataObject.Value;

                        if (changedKey == key_RelayNGOCode)
                            localLobby.RelayNGOCode.Value = playerDataObject.Value;

                        if (changedKey == key_LobbyState)
                            localLobby.LocalLobbyState.Value = (LobbyState)int.Parse(playerDataObject.Value);

                        if (changedKey == key_LobbyColor)
                            localLobby.LocalLobbyColor.Value = (LobbyColor)int.Parse(playerDataObject.Value);
                    }
                }

                void PlayersJoined()
                {
                    foreach (var playerChanges in changes.PlayerJoined.Value)
                    {
                        Player joinedPlayer = playerChanges.Player;

                        var id = joinedPlayer.Id;
                        var index = playerChanges.PlayerIndex;

                        var isHost = localLobby.HostID.Value.Equals(id);
                        var displayName = joinedPlayer.Data?.ContainsKey(key_Displayname) == true
                            ? joinedPlayer.Data[key_Displayname].Value
                            : default;
                        var emote = joinedPlayer.Data?.ContainsKey(key_Emote) == true
                            ? (EmoteType)int.Parse(joinedPlayer.Data[key_Emote].Value)
                            : EmoteType.None;
                        var userStatus = joinedPlayer.Data?.ContainsKey(key_Userstatus) == true
                            ? (UserStatus)int.Parse(joinedPlayer.Data[key_Userstatus].Value)
                            : UserStatus.Lobby;

                        var newPlayer = new LocalPlayer(id, index, isHost, displayName, emote, userStatus);
                        localLobby.AddPlayer(newPlayer);
                    }
                }

                void PlayersLeft()
                {
                    foreach (var leftPlayerIndex in changes.PlayerLeft.Value)
                    {
                        localLobby.RemovePlayer(leftPlayerIndex);
                    }
                    throw new NotImplementedException("Need to switch to Player lists");
                }

                void PlayerDataChanged()
                {
                    foreach (var lobbyPlayerChanges in changes.PlayerData.Value)
                    {
                        var playerIndex = lobbyPlayerChanges.Key;
                        var localPlayer = localLobby.GetLocalPlayer(playerIndex);
                        var playerChanges = lobbyPlayerChanges.Value;
                        if (playerChanges.ConnectionInfoChanged.Changed)
                        {
                            var connectionInfo = playerChanges.ConnectionInfoChanged.Value;
                            Debug.Log(
                                $"ConnectionInfo for {localPlayer.DisplayName.Value} changed to {connectionInfo}");
                        }

                        if (playerChanges.LastUpdatedChanged.Changed)
                        {
                            var lastUpdated = playerChanges.LastUpdatedChanged.Value;
                            Debug.Log(
                                $"ConnectionInfo for {localPlayer.DisplayName.Value} changed to {lastUpdated}");
                        }

                        if (playerChanges.ChangedData.Changed)
                        {
                            foreach (var playerChange in playerChanges.ChangedData.Value)
                            {
                                var changedValue = playerChange.Value;
                                if (changedValue.Changed)
                                {
                                    if (changedValue.Removed)
                                    {
                                        Debug.LogWarning("This Sample does not remove Values currently.");
                                        continue;
                                    }

                                    var changedKey = playerChange.Key;
                                    var playerDataObject = changedValue.Value;
                                    ParseLocalPlayerData(changedKey, playerDataObject);
                                }
                            }
                        }

                        void ParseLocalPlayerData(string dataKey, PlayerDataObject playerDataObject)
                        {
                            localPlayer.DisplayName.Value = dataKey == key_Displayname
                                ? playerDataObject.Value
                                : default;
                            localPlayer.Emote.Value = dataKey == key_Emote
                                ? (EmoteType)int.Parse(playerDataObject.Value)
                                : EmoteType.None;
                            localPlayer.UserStatus.Value = dataKey == key_Userstatus
                                ? (UserStatus)int.Parse(playerDataObject.Value)
                                : UserStatus.Lobby;
                        }
                    }
                }

                /// <summary>
                /// Takes a lobby and a change applicator to update a given lobby in-place.
                /// If LobbyDeleted is true, no changes will be applied and a warning will be logged.
                /// </summary>
                /// <param name="lobby">The lobby model to apply the changes to.</param>
                //void ApplyToLobby(Models.Lobby lobby);
            };

            m_LobbyEventCallbacks.LobbyEventConnectionStateChanged += lobbyEventConnectionState =>
            {
                Debug.Log($"Lobby Event Changed {lobbyEventConnectionState}");

            };

            m_LobbyEventCallbacks.KickedFromLobby += () =>
            {
                Debug.Log("Left Lobby");
            };
            await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobbyID, m_LobbyEventCallbacks);
        }


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

        public async Task<Lobby> GetLobbyAsync(string lobbyId = null)
        {
            if (!InLobby())
                return null;
            await m_GetLobbyCooldown.WaitUntilCooldown();
            lobbyId ??= m_CurrentLobby.Id;
            return m_CurrentLobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);
        }

        public async Task LeaveLobbyAsync()
        {
            await m_LeaveLobbyOrRemovePlayer.WaitUntilCooldown();
            if (!InLobby())
                return;
            string playerId = AuthenticationService.Instance.PlayerId;
            Debug.Log($"{playerId} leaving Lobby {m_CurrentLobby.Id}");

            await LobbyService.Instance.RemovePlayerAsync(m_CurrentLobby.Id, playerId);
            m_CurrentLobby = null;
        }

        public async Task<Lobby> UpdatePlayerDataAsync(Dictionary<string, string> data)
        {
            if (!InLobby())
                return null;
            await m_UpdatePlayerCooldown.WaitUntilCooldown();
            Debug.Log("Lobby - Updating Player Data");

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

            UpdatePlayerOptions updateOptions = new UpdatePlayerOptions
            {
                Data = dataCurr,
                AllocationId = null,
                ConnectionInfo = null
            };
            return m_CurrentLobby =
                await LobbyService.Instance.UpdatePlayerAsync(m_CurrentLobby.Id, playerId, updateOptions);
        }

        public async Task<Lobby> UpdatePlayerRelayInfoAsync(string lobbyID, string allocationId, string connectionInfo)
        {
            if (!InLobby())
                return null;
            await m_UpdatePlayerCooldown.WaitUntilCooldown();
            Debug.Log("Lobby - Relay Info (Player)");

            string playerId = AuthenticationService.Instance.PlayerId;

            UpdatePlayerOptions updateOptions = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>(),
                AllocationId = allocationId,
                ConnectionInfo = connectionInfo
            };
            return m_CurrentLobby = await LobbyService.Instance.UpdatePlayerAsync(lobbyID, playerId, updateOptions);
        }

        public async Task<Lobby> UpdateLobbyDataAsync(Dictionary<string, string> data)
        {
            if (!InLobby())
                return null;
            await m_UpdateLobbyCooldown.WaitUntilCooldown();
            Debug.Log("Lobby - Updating Lobby Data");

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

            UpdateLobbyOptions updateOptions = new UpdateLobbyOptions { Data = dataCurr, IsLocked = shouldLock };
            return m_CurrentLobby = await LobbyService.Instance.UpdateLobbyAsync(m_CurrentLobby.Id, updateOptions);
        }

        public async Task DeleteLobbyAsync()
        {
            if (!InLobby())
                return;
            await m_DeleteLobbyCooldown.WaitUntilCooldown();
            Debug.Log("Lobby - Deleting Lobby");

            await LobbyService.Instance.DeleteLobbyAsync(m_CurrentLobby.Id);
        }

        public void Dispose()
        {
            m_CurrentLobby = null;
        }

        #region HeartBeat

//Since the LobbyManager maintains the "connection" to the lobby, we will continue to heartbeat until host leaves.
        async Task SendHeartbeatPingAsync()
        {
            if (!InLobby())
                return;
            if (m_HeartBeatCooldown.IsInCooldown)
                return;
            await m_HeartBeatCooldown.WaitUntilCooldown();
            Debug.Log("Lobby - Heartbeat");

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

    //Manages the Cooldown for each service call.
    //Adds a buffer to account for ping times.
    public class RateLimiter
    {
        public Action<bool> onCooldownChange;
        public readonly float cooldownSeconds;
        public readonly int coolDownMS;
        public readonly int pingBufferMS;

        //(If you're still getting rate limit errors, try increasing the pingBuffer)
        public RateLimiter(float cooldownSeconds, int pingBuffer = 100)
        {
            this.cooldownSeconds = cooldownSeconds;
            pingBufferMS = pingBuffer;
            coolDownMS =
                Mathf.CeilToInt(this.cooldownSeconds * 1000) +
                pingBufferMS;
        }

        public async Task WaitUntilCooldown()
        {
            //No Queue!
            if (!m_IsInCooldown)
            {
#pragma warning disable 4014
                CooldownAsync();
#pragma warning restore 4014
                return;
            }

            while (m_IsInCooldown)
            {
                await Task.Delay(10);
            }
        }

        async Task CooldownAsync()
        {
            IsInCooldown = true;
            await Task.Delay(coolDownMS);
            IsInCooldown = false;
        }

        bool m_IsInCooldown = false;

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
    }
}
