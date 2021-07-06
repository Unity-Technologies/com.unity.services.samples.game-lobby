using LobbyRelaySample.Relay;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace LobbyRelaySample
{
    public class GameStateManager : MonoBehaviour, IReceiveMessages
    {
        [SerializeField]
        LogMode m_logMode = LogMode.Critical;
        /// <summary>
        /// All these should be assigned the observers in the scene at the start.
        /// </summary>
        [SerializeField]
        List<LocalGameStateObserver> m_GameStateObservers = new List<LocalGameStateObserver>();
        [SerializeField]
        List<LocalLobbyObserver> m_LocalLobbyObservers = new List<LocalLobbyObserver>();
        [SerializeField]
        List<LobbyUserObserver> m_LocalUserObservers = new List<LobbyUserObserver>();
        [SerializeField]
        List<LobbyServiceDataObserver> m_LobbyServiceObservers = new List<LobbyServiceDataObserver>();

        private LobbyContentHeartbeat m_roomsContentHeartbeat = new LobbyContentHeartbeat();

        LobbyUser m_localUser;
        LocalLobby m_localLobby;
        LobbyServiceData m_lobbyServiceData = new LobbyServiceData();
        LocalGameState m_localGameState = new LocalGameState();
        ReadyCheck m_ReadyCheck;

        public void Awake()
        {
            // Do some arbitrary operations to instantiate singletons.
            LogHandler.Get().mode = m_logMode;
#pragma warning disable IDE0059 // Unnecessary assignment of a value
            var unused = Locator.Get;
#pragma warning restore IDE0059 // Unnecessary assignment of a value
            Locator.Get.Provide(new Auth.Identity(OnAuthSignIn));
            m_ReadyCheck = new ReadyCheck(7);
            Application.wantsToQuit += OnWantToQuit;
        }

        private void OnAuthSignIn()
        {
            Debug.Log("Signed in.");
            m_localUser.ID = Locator.Get.Identity.GetSubIdentity(Auth.IIdentityType.Auth).GetContent("id");
            m_localUser.DisplayName = NameGenerator.GetName(m_localUser.ID);
        }

        public void OnReceiveMessage(MessageType type, object msg)
        {
            if (type == MessageType.RenameRequest)
            {
                m_localUser.DisplayName = (string)msg;
            }
            else if (type == MessageType.CreateLobbyRequest)
            {
                var createRoomData = (LocalLobby)msg;
                LobbyAsyncRequests.Instance.CreateLobbyAsync(createRoomData.LobbyName, createRoomData.MaxPlayerCount, createRoomData.Private, (r) =>
                {
                    Lobby.ToLocalLobby.Convert(r, m_localLobby, m_localUser);
                    OnCreatedRoom();
                }, OnFailedJoin);
            }
            else if (type == MessageType.JoinLobbyRequest)
            {
                LobbyInfo roomData = (LobbyInfo)msg;
                LobbyAsyncRequests.Instance.JoinLobbyAsync(roomData.LobbyID, roomData.LobbyCode, (r) =>
                {
                    Lobby.ToLocalLobby.Convert(r, m_localLobby, m_localUser);
                    OnJoinedRoom();
                }, OnFailedJoin);
            }
            else if (type == MessageType.QueryLobbies)
            {
                m_lobbyServiceData.State = LobbyServiceState.Fetching;
                LobbyAsyncRequests.Instance.RetrieveLobbyListAsync(
                    qr =>
                    {
                        if (qr != null)
                            OnRefreshed(Lobby.ToLocalLobby.Convert(qr));
                    }, er =>
                    {
                        long errorLong = 0;
                        if (er != null)
                            errorLong = er.Status;
                        OnRefreshFailed(errorLong);
                    });
            }
            else if (type == MessageType.ChangeGameState)
            {
                SetGameState((GameState)msg);
            }
            else if (type == MessageType.UserSetEmote)
            {
                var emote = (string)msg;
                m_localUser.Emote = emote;
            }
            else if (type == MessageType.ChangeLobbyUserState)
            {
                m_localUser.UserStatus = (UserStatus)msg;
            }
            else if (type == MessageType.Client_EndReadyCountdownAt)
            {
                m_localLobby.TargetEndTime = (DateTime)msg;
                BeginCountDown();
            }
            else if (type == MessageType.ToLobby)
            {
                ToLobby();
            }
        }

        void Start()
        {
            m_localLobby = new LocalLobby
            {
                State = LobbyState.Lobby
            };
            m_localUser = new LobbyUser();
            m_localUser.DisplayName = "New Player";
            Locator.Get.Messenger.Subscribe(this);
            DefaultObserverSetup();
            InitObservers();
        }

        /// <summary>
        /// We find and validate that the scene has all the Observers we expect
        /// </summary>
        void DefaultObserverSetup()
        {
            foreach (var gameStateObs in FindObjectsOfType<LocalGameStateObserver>())
            {
                if (!gameStateObs.observeOnStart)
                    continue;

                if (!m_GameStateObservers.Contains(gameStateObs))
                    m_GameStateObservers.Add(gameStateObs);
            }

            foreach (var localLobby in FindObjectsOfType<LocalLobbyObserver>())
            {
                if (!localLobby.observeOnStart)
                    continue;
                if (!m_LocalLobbyObservers.Contains(localLobby))
                    m_LocalLobbyObservers.Add(localLobby);
            }

            foreach (var lobbyUserObs in FindObjectsOfType<LobbyUserObserver>())
            {
                if (!lobbyUserObs.observeOnStart)
                    continue;
                if (!m_LocalUserObservers.Contains(lobbyUserObs))
                    m_LocalUserObservers.Add(lobbyUserObs);
            }

            foreach (var lobbyServiceObs in FindObjectsOfType<LobbyServiceDataObserver>())
            {
                if (!lobbyServiceObs.observeOnStart)
                    continue;

                if (!m_LobbyServiceObservers.Contains(lobbyServiceObs))
                    m_LobbyServiceObservers.Add(lobbyServiceObs);
            }

            if (m_GameStateObservers.Count < 4)
                Debug.LogWarning($"Scene has less than the default expected Game State Observers, ensure all the observers in the scene that need to watch the gameState are registered in the LocalGameStateObservers List.");

            if (m_LocalLobbyObservers.Count < 8)
                Debug.LogWarning($"Scene has less than the default expected Local Lobby Observers, ensure all the observers in the scene that need to watch the Local Lobby are registered in the LocalLobbyObservers List.");

            if (m_LocalUserObservers.Count < 3)
                Debug.LogWarning($"Scene has less than the default expected Local User Observers, ensure all the observers in the scene that need to watch the gameState are registered in the LocalUserObservers List.");

            if (m_LobbyServiceObservers.Count < 2)
                Debug.LogWarning($"Scene has less than the default expected Lobby Service Observers, ensure all the observers in the scene that need to watch the lobby service state  are registered in the LobbyServiceObservers List.");
        }

        void InitObservers()
        {
            foreach (var gameStateObs in m_GameStateObservers)
            {
                if (gameStateObs == null)
                {
                    Debug.LogError("Missing a gameStateObserver, please make sure all GameStateObservers in the scene are registered here.");
                    continue;
                }

                gameStateObs.BeginObserving(m_localGameState);
            }

            foreach (var lobbyObs in m_LocalLobbyObservers)
            {
                if (lobbyObs == null)
                {
                    Debug.LogError("Missing a gameStateObserver, please make sure all GameStateObservers in the scene are registered here.");
                    continue;
                }

                lobbyObs.BeginObserving(m_localLobby);
            }

            foreach (var userObs in m_LocalUserObservers)
            {
                if (userObs == null)
                {
                    Debug.LogError("Missing a gameStateObserver, please make sure all GameStateObservers in the scene are registered here.");
                    continue;
                }

                userObs.BeginObserving(m_localUser);
            }

            foreach (var serviceObs in m_LobbyServiceObservers)
            {
                if (serviceObs == null)
                {
                    Debug.LogError("Missing a gameStateObserver, please make sure all GameStateObservers in the scene are registered here.");
                    continue;
                }

                serviceObs.BeginObserving(m_lobbyServiceData);
            }
        }

        void SetGameState(GameState state)
        {
            bool isLeavingRoom = (state == GameState.Menu || state == GameState.JoinMenu) && m_localGameState.State == GameState.Lobby;
            m_localGameState.State = state;
            if (isLeavingRoom)
                OnLeftRoom();
        }

        void OnRefreshed(IEnumerable<LocalLobby> lobbies)
        {
            var newLobbyDict = new Dictionary<string, LocalLobby>();
            foreach (var lobby in lobbies)
            {
                newLobbyDict.Add(lobby.LobbyID, lobby);
            }

            m_lobbyServiceData.State = LobbyServiceState.Fetched;
            m_lobbyServiceData.CurrentLobbies = newLobbyDict;
        }

        void OnRefreshFailed(long errorCode)
        {
            m_lobbyServiceData.lastErrorCode = errorCode;
            m_lobbyServiceData.State = LobbyServiceState.Error;
        }

        void OnCreatedRoom()
        {
            OnJoinedRoom();
        }

        void OnGotRelayAllocation(Allocation allocationID)
        {
            RelayInterface.GetJoinCodeAsync(allocationID.AllocationId, OnGotRelayCode);
        }

        void OnGotRelayCode(string relayCode)
        {
            m_localLobby.RelayCode = relayCode;
        }

        void OnJoinedRoom()
        {
            LobbyAsyncRequests.Instance.BeginTracking(m_localLobby.LobbyID);
            m_roomsContentHeartbeat.BeginTracking(m_localLobby, m_localUser);
            SetUserLobbyState();
            Dictionary<string, string> displayNameData = new Dictionary<string, string>();
            displayNameData.Add("DisplayName", m_localUser.DisplayName);
            LobbyAsyncRequests.Instance.UpdatePlayerDataAsync(displayNameData, null);
        }

        void OnLeftRoom()
        {
            m_localUser.Emote = null;
            LobbyAsyncRequests.Instance.LeaveLobbyAsync(m_localLobby.LobbyID, ResetLocalLobby);
            m_roomsContentHeartbeat.EndTracking();
            LobbyAsyncRequests.Instance.EndTracking();
        }

        /// <summary>
        /// Back to Join menu if we fail to join for whatever reason.
        /// </summary>
        void OnFailedJoin()
        {
            SetGameState(GameState.JoinMenu);
        }

        /// <summary>
        /// We do the Relay server Allocations right before we do the relay join allocations, as waiting too long will
        /// cause the relay server to get cleand up by the service
        /// </summary>
        void BeginCountDown()
        {
            // Only start the countdown once.
            if (m_localLobby.State == LobbyState.CountDown)
                return;
            RelayInterface.AllocateAsync(m_localLobby.MaxPlayerCount, OnGotRelayAllocation);
            m_localLobby.CountDownTime = m_localLobby.TargetEndTime.Subtract(DateTime.Now).Seconds;
            m_localLobby.State = LobbyState.CountDown;
            StartCoroutine(CountDown());
        }

        IEnumerator CountDown()
        {
            m_ReadyCheck.EndCheckingForReady();
            while (m_localLobby.CountDownTime > 0)
            {
                yield return new WaitForSeconds(0.2f);
                if (m_localLobby.State != LobbyState.CountDown)
                    yield break;
                m_localLobby.CountDownTime = m_localLobby.TargetEndTime.Subtract(DateTime.Now).Seconds;
            }

            m_localUser.UserStatus = UserStatus.Connecting;
            m_localLobby.State = LobbyState.InGame;

            RelayInterface.JoinAsync(m_localLobby.RelayCode, OnJoinedGame);
        }

        void OnJoinedGame(JoinAllocation joinData)
        {
            m_localUser.UserStatus = UserStatus.Connected;
            var ip = joinData.RelayServer.IpV4;
            var port = joinData.RelayServer.Port;
            m_localLobby.RelayServer = new ServerAddress(ip, port);
        }

        void ToLobby()
        {
            m_localLobby.State = LobbyState.Lobby;
            m_localLobby.CountDownTime = 0;
            m_localLobby.RelayServer = null;
            m_localLobby.RelayCode = null;
            SetUserLobbyState();
        }

        void SetUserLobbyState()
        {
            SetGameState(GameState.Lobby);
            m_localUser.UserStatus = UserStatus.Lobby;
            if (m_localUser.IsHost)
                m_ReadyCheck.BeginCheckingForReady();
        }

        void ResetLocalLobby()
        {
            m_localLobby.CopyObserved(new LobbyInfo(), new Dictionary<string, LobbyUser>());
            m_localLobby.CountDownTime = 0;
            m_localLobby.RelayServer = null;
            m_ReadyCheck.EndCheckingForReady();
        }

        void OnDestroy()
        {
            ForceLeaveAttempt();
        }

        bool OnWantToQuit()
        {
            bool canQuit = string.IsNullOrEmpty(m_localLobby?.LobbyID);
            StartCoroutine(LeaveBeforeQuit());
            return canQuit;
        }

        void ForceLeaveAttempt()
        {
            Locator.Get.Messenger.Unsubscribe(this);
            if (!string.IsNullOrEmpty(m_localLobby?.LobbyID))
            {
                LobbyAsyncRequests.Instance.LeaveLobbyAsync(m_localLobby?.LobbyID, null);
                m_localLobby = null;
            }
        }

        /// <summary>
        /// In builds, if we are in a room and try to send a Leave request on application quit, it won't go through if we're quitting on the same frame.
        /// So, we need to delay just briefly to let the request happen (though we don't need to wait for the result).
        /// </summary>
        IEnumerator LeaveBeforeQuit()
        {
            ForceLeaveAttempt();

            // TEMP: Since we're temporarily (as of 6/31/21) deleting empty rooms when we leave them manually, we'll delay a bit to ensure that happens.
            //yield return null;
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }
    }
}
