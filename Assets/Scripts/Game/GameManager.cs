using System;
using LobbyRelaySample.Relay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LobbyRelaySample
{
    /// <summary>
    /// Sets up and runs the entire sample.
    /// </summary>
    public class GameManager : MonoBehaviour, IReceiveMessages
    {
        /// <summary>
        /// All these should be assigned the observers in the scene at the start.
        /// </summary>

        #region UI elements that observe the local state. These are 

        [SerializeField]
        private List<LocalGameStateObserver> m_GameStateObservers = new List<LocalGameStateObserver>();
        [SerializeField]
        private List<LocalLobbyObserver> m_LocalLobbyObservers = new List<LocalLobbyObserver>();
        [SerializeField]
        private List<LobbyUserObserver> m_LocalUserObservers = new List<LobbyUserObserver>();
        [SerializeField]
        private List<LobbyServiceDataObserver> m_LobbyServiceObservers = new List<LobbyServiceDataObserver>();

        #endregion

        private LocalGameState m_localGameState = new LocalGameState();
        private LobbyUser m_localUser;
        private LocalLobby m_localLobby;
        private LobbyServiceData m_lobbyServiceData = new LobbyServiceData();
        private LobbyContentHeartbeat m_lobbyContentHeartbeat = new LobbyContentHeartbeat();
        private RelayUtpSetup m_relaySetup;
        private RelayUtpClient m_relayClient;

        // The Lobby API rate limits query requests to one every 1.5s, and it will return a 429 "Too Many Requests" error otherwise.
        private const float k_lobbyAssignmentCoolingSeconds = 1.5f;
        private bool m_coolingDown;

        /// <summary>Rather than a setter, this is usable in-editor. It won't accept an enum, however.</summary>
        public void SetLobbyColorFilter(int color)
        {
            m_lobbyColorFilter = (LobbyColor)color;
        }

        private LobbyColor m_lobbyColorFilter;

        #region Setup

        private void Awake()
        {
            // Do some arbitrary operations to instantiate singletons.
#pragma warning disable IDE0059 // Unnecessary assignment of a value
            var unused = Locator.Get;
#pragma warning restore IDE0059

            Locator.Get.Provide(new Auth.Identity(OnAuthSignIn));
            Application.wantsToQuit += OnWantToQuit;
        }

        private void Start()
        {
            m_localLobby = new LocalLobby { State = LobbyState.Lobby };
            m_localUser = new LobbyUser();
            m_localUser.DisplayName = "New Player";
            Locator.Get.Messenger.Subscribe(this);
            BeginObservers();
        }

        private void OnAuthSignIn()
        {
            Debug.Log("Signed in.");
            m_localUser.ID = Locator.Get.Identity.GetSubIdentity(Auth.IIdentityType.Auth).GetContent("id");
            m_localUser.DisplayName = NameGenerator.GetName(m_localUser.ID);
            m_localLobby.AddPlayer(m_localUser); // The local LobbyUser object will be hooked into UI before the LocalLobby is populated during lobby join, so the LocalLobby must know about it already when that happens.
        }

        private void BeginObservers()
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

        #endregion

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
                if (!LobbyRequestCooledDown())
                    return;

                var createLobbyData = (LocalLobby)msg;
                LobbyAsyncRequests.Instance.CreateLobbyAsync(createLobbyData.LobbyName, createLobbyData.MaxPlayerCount, createLobbyData.Private, m_localUser, (r) =>
                    {
                        lobby.ToLocalLobby.Convert(r, m_localLobby);
                        OnCreatedLobby();
                    },
                    OnFailedJoin);
            }
            else if (type == MessageType.JoinLobbyRequest)
            {
                if (!LobbyRequestCooledDown())
                    return;

                LocalLobby.LobbyData lobbyInfo = (LocalLobby.LobbyData)msg;
                LobbyAsyncRequests.Instance.JoinLobbyAsync(lobbyInfo.LobbyID, lobbyInfo.LobbyCode, m_localUser, (r) =>
                    {
                        lobby.ToLocalLobby.Convert(r, m_localLobby);
                        OnJoinedLobby();
                    },
                    OnFailedJoin);
            }
            else if (type == MessageType.QueryLobbies)
            {
                if (!LobbyRequestCooledDown())
                    return;
                m_lobbyServiceData.State = LobbyQueryState.Fetching;
                LobbyAsyncRequests.Instance.RetrieveLobbyListAsync(
                    qr =>
                    {
                        if (qr != null)
                            OnLobbiesQueried(lobby.ToLocalLobby.Convert(qr));
                    },
                    er =>
                    {
                        long errorLong = 0;
                        if (er != null)
                            errorLong = er.Status;
                        OnLobbyQueryFailed(errorLong);
                    },
                    m_lobbyColorFilter);
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

        private void SetGameState(GameState state)
        {
            bool isLeavingLobby = (state == GameState.Menu || state == GameState.JoinMenu) && m_localGameState.State == GameState.Lobby;
            m_localGameState.State = state;
            if (isLeavingLobby)
                OnLeftLobby();
        }

        private void OnLobbiesQueried(IEnumerable<LocalLobby> lobbies)
        {
            var newLobbyDict = new Dictionary<string, LocalLobby>();
            foreach (var lobby in lobbies)
                newLobbyDict.Add(lobby.LobbyID, lobby);

            m_lobbyServiceData.State = LobbyQueryState.Fetched;
            m_lobbyServiceData.CurrentLobbies = newLobbyDict;
        }

        private void OnLobbyQueryFailed(long errorCode)
        {
            m_lobbyServiceData.lastErrorCode = errorCode;
            m_lobbyServiceData.State = LobbyQueryState.Error;
        }

        private void OnCreatedLobby()
        {
            m_localUser.IsHost = true;
            OnJoinedLobby();
        }

        private void OnJoinedLobby()
        {
            LobbyAsyncRequests.Instance.BeginTracking(m_localLobby.LobbyID);
            m_lobbyContentHeartbeat.BeginTracking(m_localLobby, m_localUser);
            SetUserLobbyState();
            StartRelayConnection();
        }

        private void OnLeftLobby()
        {
            m_localUser.ResetState();
            LobbyAsyncRequests.Instance.LeaveLobbyAsync(m_localLobby.LobbyID, ResetLocalLobby);
            m_lobbyContentHeartbeat.EndTracking();
            LobbyAsyncRequests.Instance.EndTracking();

            if (m_relaySetup != null)
            {
                Component.Destroy(m_relaySetup);
                m_relaySetup = null;
            }

            if (m_relayClient != null)
            {
                Component.Destroy(m_relayClient);
                m_relayClient = null;
            }
        }

        private bool LobbyRequestCooledDown()
        {
            if (m_coolingDown)
                return false;
            StartCoroutine(RequestCoolDown());
            return true;
        }

        private IEnumerator RequestCoolDown()
        {
            m_coolingDown = true;
            yield return new WaitForSeconds(k_lobbyAssignmentCoolingSeconds);
            m_coolingDown = false;
        }

        /// <summary>
        /// Back to Join menu if we fail to join for whatever reason.
        /// </summary>
        private void OnFailedJoin()
        {
            SetGameState(GameState.JoinMenu);
        }

        private void StartRelayConnection()
        {
            if (m_localUser.IsHost)
                m_relaySetup = gameObject.AddComponent<RelayUtpSetupHost>();
            else
                m_relaySetup = gameObject.AddComponent<RelayUtpSetupClient>();
            OnReceiveMessage(MessageType.LobbyUserStatus, UserStatus.Connecting);
            m_relaySetup.BeginRelayJoin(m_localLobby, m_localUser, OnRelayConnected);
        }

        private void OnRelayConnected(bool didSucceed, RelayUtpClient client)
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

        private IEnumerator RetryRelayConnection()
        {
            yield return new WaitForSeconds(5);
            StartRelayConnection();
        }

        private void BeginCountDown()
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
        private IEnumerator CountDown()
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

        private void SetUserLobbyState()
        {
            SetGameState(GameState.Lobby);
            OnReceiveMessage(MessageType.LobbyUserStatus, UserStatus.Lobby);
        }

        private void ResetLocalLobby()
        {
            m_localLobby.CopyObserved(new LocalLobby.LobbyData(), new Dictionary<string, LobbyUser>());
            m_localLobby.AddPlayer(m_localUser); // As before, the local player will need to be plugged into UI before the lobby join actually happens.
            m_localLobby.CountDownTime = 0;
            m_localLobby.RelayServer = null;
        }

        #region Teardown

        /// <summary>
        /// In builds, if we are in a lobby and try to send a Leave request on application quit, it won't go through if we're quitting on the same frame.
        /// So, we need to delay just briefly to let the request happen (though we don't need to wait for the result).
        /// </summary>
        private IEnumerator LeaveBeforeQuit()
        {
            ForceLeaveAttempt();
            yield return null;
            Application.Quit();
        }

        private bool OnWantToQuit()
        {
            bool canQuit = string.IsNullOrEmpty(m_localLobby?.LobbyID);
            StartCoroutine(LeaveBeforeQuit());
            return canQuit;
        }

        private void OnDestroy()
        {
            ForceLeaveAttempt();
        }

        private void ForceLeaveAttempt()
        {
            Locator.Get.Messenger.Unsubscribe(this);
            if (!string.IsNullOrEmpty(m_localLobby?.LobbyID))
            {
                LobbyAsyncRequests.Instance.LeaveLobbyAsync(m_localLobby?.LobbyID, null);
                m_localLobby = null;
            }
        }

        #endregion
    }
}
