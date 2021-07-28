using LobbyRelaySample.Relay;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LobbyRelaySample
{
    // TODO: This is pretty bloated. Additionally, it needs a pass for removing redundant calls and organizing things in a more intuitive way and whatnot

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

        private LobbyContentHeartbeat m_lobbyContentHeartbeat = new LobbyContentHeartbeat();

        LobbyUser m_localUser;
        LocalLobby m_localLobby;
        LobbyServiceData m_lobbyServiceData = new LobbyServiceData();
        LocalGameState m_localGameState = new LocalGameState();

        RelayUtpSetup m_relaySetup;
        RelayUtpClient m_relayClient;

        public void Awake()
        {
            LogHandler.Get().mode = m_logMode;
            // Do some arbitrary operations to instantiate singletons.
#pragma warning disable IDE0059 // Unnecessary assignment of a value
            var unused = Locator.Get;
#pragma warning restore IDE0059 // Unnecessary assignment of a value
            Locator.Get.Provide(new Auth.Identity(OnAuthSignIn));
            Application.wantsToQuit += OnWantToQuit;
        }

        private void OnAuthSignIn()
        {
            Debug.Log("Signed in.");
            m_localUser.ID = Locator.Get.Identity.GetSubIdentity(Auth.IIdentityType.Auth).GetContent("id");
            m_localUser.DisplayName = NameGenerator.GetName(m_localUser.ID);
            m_localLobby.AddPlayer(m_localUser); // The local LobbyUser object will be hooked into UI before the LocalLobby is populated during lobby join, so the LocalLobby must know about it already when that happens.
        }

        /// <summary>
        /// Primarily used for UI elements to communicate state changes, this will receive messages from arbitrary providers for user interactions.
        /// </summary>
        public void OnReceiveMessage(MessageType type, object msg)
        {
            if (type == MessageType.RenameRequest)
            {
                m_localUser.DisplayName = (string)msg;
            }
            else if (type == MessageType.CreateLobbyRequest)
            {
                var createLobbyData = (LocalLobby)msg;
                LobbyAsyncRequests.Instance.CreateLobbyAsync(createLobbyData.LobbyName, createLobbyData.MaxPlayerCount, createLobbyData.Private, m_localUser, (r) =>
                {
                    lobby.ToLocalLobby.Convert(r, m_localLobby);
                    OnCreatedLobby();
                }, OnFailedJoin);
            }
            else if (type == MessageType.JoinLobbyRequest)
            {
                LocalLobby.LobbyData lobbyInfo = (LocalLobby.LobbyData)msg;
                LobbyAsyncRequests.Instance.JoinLobbyAsync(lobbyInfo.LobbyID, lobbyInfo.LobbyCode, m_localUser, (r) =>
                {
                    lobby.ToLocalLobby.Convert(r, m_localLobby);
                    OnJoinedLobby();
                }, OnFailedJoin);
            }
            else if (type == MessageType.QueryLobbies)
            {
                m_lobbyServiceData.State = LobbyServiceState.Fetching;
                LobbyAsyncRequests.Instance.RetrieveLobbyListAsync(
                    qr =>
                    {
                        if (qr != null)
                            OnRefreshed(lobby.ToLocalLobby.Convert(qr));
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
                EmoteType emote = (EmoteType)msg;
                m_localUser.Emote = emote;
            }
            else if (type == MessageType.LobbyUserStatus)
            {
                m_localUser.UserStatus = (UserStatus)msg;
            }
            else if (type == MessageType.StartCountdown)
            {
                BeginCountDown();
            }
            else if (type == MessageType.CancelCountdown)
            {
                m_localLobby.State = LobbyState.Lobby;
                m_localLobby.CountDownTime = 0;
            }
            else if (type == MessageType.ConfirmInGameState)
            {
                m_localUser.UserStatus = UserStatus.InGame;
                m_localLobby.State = LobbyState.InGame;
            }
            else if (type == MessageType.EndGame)
            {
                m_localLobby.State = LobbyState.Lobby;
                m_localLobby.CountDownTime = 0;
                SetUserLobbyState();
            }
        }

        void Start()
        {
            m_localLobby = new LocalLobby { State = LobbyState.Lobby };
            m_localUser = new LobbyUser();
            m_localUser.DisplayName = "New Player";
            Locator.Get.Messenger.Subscribe(this);
            BeginObservers();
        }

        void BeginObservers()
        {
            foreach (var gameStateObs in m_GameStateObservers)
                gameStateObs.BeginObserving(m_localGameState);
            foreach (var serviceObs in m_LobbyServiceObservers)
                serviceObs.BeginObserving(m_lobbyServiceData);
            foreach (var lobbyObs in m_LocalLobbyObservers)
                lobbyObs.BeginObserving(m_localLobby);
            foreach (var userObs in m_LocalUserObservers)
                userObs.BeginObserving(m_localUser);
        }

        void SetGameState(GameState state)
        {
            bool isLeavingLobby = (state == GameState.Menu || state == GameState.JoinMenu) && m_localGameState.State == GameState.Lobby;
            m_localGameState.State = state;
            if (isLeavingLobby)
                OnLeftLobby();
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

        void OnCreatedLobby()
        {
            OnJoinedLobby();
        }

        void OnJoinedLobby()
        {
            LobbyAsyncRequests.Instance.BeginTracking(m_localLobby.LobbyID);
            m_lobbyContentHeartbeat.BeginTracking(m_localLobby, m_localUser);
            SetUserLobbyState();
            StartRelayConnection();
        }

        void StartRelayConnection()
        {
            if (m_localUser.IsHost)
                m_relaySetup = gameObject.AddComponent<RelayUtpSetupHost>();
            else
                m_relaySetup = gameObject.AddComponent<RelayUtpSetupClient>();
            OnReceiveMessage(MessageType.LobbyUserStatus, UserStatus.Connecting);
            m_relaySetup.BeginRelayJoin(m_localLobby, m_localUser, OnRelayConnected);
        }

        void OnRelayConnected(bool didSucceed, RelayUtpClient client)
        {
            Component.Destroy(m_relaySetup);
            m_relaySetup = null;

            if (!didSucceed)
            {
                Debug.LogError("Relay connection failed! Retrying in 5s...");
                StartCoroutine(RetryRelayConnection());
                return;
            }
            m_relayClient = client;
            OnReceiveMessage(MessageType.LobbyUserStatus, UserStatus.Lobby);
        }

        IEnumerator RetryRelayConnection()
        {
            yield return new WaitForSeconds(5);
            StartRelayConnection();
        }

        void OnLeftLobby()
        {
            m_localUser.Emote = EmoteType.None;
            LobbyAsyncRequests.Instance.LeaveLobbyAsync(m_localLobby.LobbyID, ResetLocalLobby);
            m_lobbyContentHeartbeat.EndTracking();
            LobbyAsyncRequests.Instance.EndTracking();

            if (m_relaySetup != null)
            {   Component.Destroy(m_relaySetup);
                m_relaySetup = null;
            }
            if (m_relayClient != null)
            {   Component.Destroy(m_relayClient);
                m_relayClient = null;
            }
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
            if (m_localLobby.State == LobbyState.CountDown)
                return;
            m_localLobby.CountDownTime = 4;
            m_localLobby.State = LobbyState.CountDown;
            StartCoroutine(CountDown());
        }
        
        /// <summary>
        /// The CountdownUI will pick up on changes to the lobby's countdown timer. This can be interrupted if the lobby leaves the countdown state (via a CancelCountdown message).
        /// </summary>
        IEnumerator CountDown()
        {
            while (m_localLobby.CountDownTime > 0)
            {
                yield return null;
                if (m_localLobby.State != LobbyState.CountDown)
                    yield break;
                m_localLobby.CountDownTime -= Time.deltaTime;
            }
            if (m_relayClient is RelayUtpHost)
                (m_relayClient as RelayUtpHost).SendInGameState();
        }

        void SetUserLobbyState()
        {
            SetGameState(GameState.Lobby);
            OnReceiveMessage(MessageType.LobbyUserStatus, UserStatus.Lobby);
        }

        void ResetLocalLobby()
        {
            m_localLobby.CopyObserved(new LocalLobby.LobbyData(), new Dictionary<string, LobbyUser>());
            m_localLobby.CountDownTime = 0;
            m_localLobby.RelayServer = null;
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
        /// In builds, if we are in a lobby and try to send a Leave request on application quit, it won't go through if we're quitting on the same frame.
        /// So, we need to delay just briefly to let the request happen (though we don't need to wait for the result).
        /// </summary>
        IEnumerator LeaveBeforeQuit()
        {
            ForceLeaveAttempt();

            // TEMP: Since we're temporarily (as of 6/31/21) deleting empty lobbies when we leave them manually, we'll delay longer to ensure that happens.
            //yield return null;
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }
    }
}
