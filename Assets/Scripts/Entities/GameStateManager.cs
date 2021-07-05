using System.Collections;
using System;
using System.Collections.Generic;
using LobbyRooms.Relay;
using Player;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement; // TODO: Definitely shouldn't need this just for logging?
using Utilities;

namespace LobbyRooms
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
        List<LobbyDataObserver> m_LobbyDataObservers = new List<LobbyDataObserver>();
        [SerializeField]
        List<LobbyUserObserver> m_LocalUserObservers = new List<LobbyUserObserver>();
        [SerializeField]
        List<LobbyServiceDataObserver> m_LobbyServiceObservers = new List<LobbyServiceDataObserver>();

        private RoomsContentHeartbeat m_roomsContentHeartbeat = new RoomsContentHeartbeat();

        LobbyUser m_localUser;
        LobbyData m_lobbyData;
        LobbyServiceData m_lobbyServiceData = new LobbyServiceData();
        LocalGameState m_localGameState = new LocalGameState();
        LobbyReadyCheck m_LobbyReadyCheck;

        public void Awake()
        {
            // Do some arbitrary operations to instantiate singletons.
            LogHandler.Get().mode = m_logMode;
#pragma warning disable IDE0059 // Unnecessary assignment of a value
            var unused = Locator.Get;
#pragma warning restore IDE0059 // Unnecessary assignment of a value
            Locator.Get.Provide(new Auth.Identity(OnAuthSignIn));
            m_LobbyReadyCheck = new LobbyReadyCheck(null, 7);
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
            else if (type == MessageType.CreateRoomRequest)
            {
                var createRoomData = (LobbyData)msg;
                RoomsQuery.Instance.CreateRoomAsync(createRoomData.LobbyName, createRoomData.MaxPlayerCount, createRoomData.Private, (r) =>
                {
                    Rooms.ToLobbyData.Convert(r, m_lobbyData, m_localUser);
                    OnCreatedRoom();
                }, OnFailedJoin); // TODO: Report failure?
            }
            else if (type == MessageType.JoinRoomRequest)
            {
                LobbyInfo roomData = (LobbyInfo)msg;
                RoomsQuery.Instance.JoinRoomAsync(roomData.RoomID, roomData.RoomCode, (r) =>
                {
                    Rooms.ToLobbyData.Convert(r, m_lobbyData, m_localUser);
                    OnJoinedRoom();
                }, OnFailedJoin); // TODO: Report failure?
            }
            else if (type == MessageType.QueryRooms)
            {
                m_lobbyServiceData.State = LobbyServiceState.Fetching;
                RoomsQuery.Instance.RetrieveRoomListAsync(
                    qr =>
                    {
                        if (qr != null)
                            OnRefreshed(Rooms.ToLobbyData.Convert(qr));
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
                m_lobbyData.TargetEndTime = (DateTime)msg;
                BeginCountDown();
            }
            else if (type == MessageType.ToLobby)
            {
                ToLobby();
            }
        }

        void Start()
        {
            m_lobbyData = new LobbyData
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

            foreach (var lobbyData in FindObjectsOfType<LobbyDataObserver>())
            {
                if (!lobbyData.observeOnStart)
                    continue;
                if (!m_LobbyDataObservers.Contains(lobbyData))
                    m_LobbyDataObservers.Add(lobbyData);
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
                Debug.LogWarning($"Scene: {SceneManager.GetActiveScene().name} has less than the default expected Game State Observers, ensure all the observers in the scene that need to watch the gameState are registered in the LocalGameStateObservers List.");

            if (m_LobbyDataObservers.Count < 8)
                Debug.LogWarning($"Scene: {SceneManager.GetActiveScene().name} has less than the default expected Lobby Data Observers, ensure all the observers in the scene that need to watch the Local Lobby Data are registered in the LobbyDataObservers List.");

            if (m_LocalUserObservers.Count < 3)
                Debug.LogWarning($"Scene: {SceneManager.GetActiveScene().name}  has less than the default expected Local User Observers, ensure all the observers in the scene that need to watch the gameState are registered in the LocalUserObservers List.");

            if (m_LobbyServiceObservers.Count < 2)
                Debug.LogWarning($"Scene: {SceneManager.GetActiveScene().name}  has less than the default expected Lobby Service Observers, ensure all the observers in the scene that need to watch the lobby service state  are registered in the LobbyServiceObservers List.");
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

            foreach (var lobbyObs in m_LobbyDataObservers)
            {
                if (lobbyObs == null)
                {
                    Debug.LogError("Missing a gameStateObserver, please make sure all GameStateObservers in the scene are registered here.");
                    continue;
                }

                lobbyObs.BeginObserving(m_lobbyData);
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
        
        void OnRefreshed(IEnumerable<LobbyData> lobbies)
        {
            var newLobbyDict = new Dictionary<string, LobbyData>();
            foreach (var lobby in lobbies)
            {
                newLobbyDict.Add(lobby.RoomID, lobby);
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
            RelayInterface.AllocateAsync(m_lobbyData.MaxPlayerCount, OnGotRelayAllocation);
        }

        void OnGotRelayAllocation(Allocation allocationID)
        {
            RelayInterface.GetJoinCodeAsync(allocationID.AllocationId, OnGotRelayCode);
        }

        void OnGotRelayCode(string relayCode)
        {
            m_lobbyData.RelayCode = relayCode;
        }

        void OnJoinedRoom()
        {
            RoomsQuery.Instance.BeginTracking(m_lobbyData.RoomID);
            m_roomsContentHeartbeat.BeginTracking(m_lobbyData, m_localUser);
            SetUserLobbyState();
            Dictionary<string, string> displayNameData = new Dictionary<string, string>();
            displayNameData.Add("DisplayName", m_localUser.DisplayName);
            RoomsQuery.Instance.UpdatePlayerDataAsync(displayNameData, null);
        }

        void OnLeftRoom()
        {
            m_localUser.Emote = null;
            RoomsQuery.Instance.LeaveRoomAsync(m_lobbyData.RoomID, ResetLobbyData);
            m_roomsContentHeartbeat.EndTracking();
            RoomsQuery.Instance.EndTracking();
        }

        /// <summary>
        /// Back to Join menu if we fail to join for whatever reason.
        /// </summary>
        void OnFailedJoin()
        {
            SetGameState(GameState.JoinMenu);
        }
        
        void BeginCountDown()
        {
            // Only start the countdown once.
            if (m_lobbyData.State == LobbyState.CountDown)
                return;
            m_lobbyData.CountDownTime = m_lobbyData.TargetEndTime.Subtract(DateTime.Now).Seconds;
            m_lobbyData.State = LobbyState.CountDown;
            StartCoroutine(CountDown());
        }
        
        IEnumerator CountDown()
        {
            m_LobbyReadyCheck.EndCheckingForReady();
            while (m_lobbyData.CountDownTime > 0)
            {
                yield return new WaitForSeconds(0.2f);
                if (m_lobbyData.State != LobbyState.CountDown)
                    yield break;
                m_lobbyData.CountDownTime = m_lobbyData.TargetEndTime.Subtract(DateTime.Now).Seconds;
            }

            m_localUser.UserStatus = UserStatus.Connecting;
            m_lobbyData.State = LobbyState.InGame;
            
            RelayInterface.JoinAsync(m_lobbyData.RelayCode, OnJoinedGame);
        }
        
        void OnJoinedGame(JoinAllocation joinData)
        {
            m_localUser.UserStatus = UserStatus.Connected;
            var ip = joinData.RelayServer.IpV4;
            var port = joinData.RelayServer.Port;
            m_lobbyData.RelayServer = new ServerAddress(ip, port);
        }

        void ToLobby()
        {
            m_lobbyData.State = LobbyState.Lobby;
            m_lobbyData.CountDownTime = 0;
            m_lobbyData.RelayServer = null;
            SetUserLobbyState();
        }

        void SetUserLobbyState()
        {
            SetGameState(GameState.Lobby);
            m_localUser.UserStatus = UserStatus.Lobby;
            if (m_localUser.IsHost)
                m_LobbyReadyCheck.BeginCheckingForReady();
        }

        void ResetLobbyData()
        {
            m_lobbyData.CopyObserved(new LobbyInfo(), new Dictionary<string, LobbyUser>());
            m_lobbyData.CountDownTime = 0;
            m_lobbyData.RelayServer = null;
            m_LobbyReadyCheck.EndCheckingForReady();
        }

        void OnDestroy()
        {
            ForceLeaveAttempt();
        }

        bool OnWantToQuit()
        {
            bool canQuit = string.IsNullOrEmpty(m_lobbyData?.RoomID);
            StartCoroutine(LeaveBeforeQuit());
            return canQuit;
        }

        void ForceLeaveAttempt()
        {
            Locator.Get.Messenger.Unsubscribe(this);
            if (!string.IsNullOrEmpty(m_lobbyData?.RoomID))
            {
                RoomsQuery.Instance.LeaveRoomAsync(m_lobbyData?.RoomID, null);
                m_lobbyData = null;
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
