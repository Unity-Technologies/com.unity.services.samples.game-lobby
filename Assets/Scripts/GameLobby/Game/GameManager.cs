using LobbyRelaySample.relay;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using LobbyRelaySample.lobby;
using Unity.Services.Authentication;
using UnityEngine;
#if UNITY_EDITOR
using ParrelSync;
#endif


namespace LobbyRelaySample
{
    /// <summary>
    /// Sets up and runs the entire sample.
    /// All the Data that is important gets updated in here, the GameManager in the mainScene has all the references
    /// needed to run the game.
    /// </summary>
    public class GameManager : MonoBehaviour, IReceiveMessages
    {
        #region UI elements that observe the local state. These should be assigned the observers in the scene during Start.

        /// <summary>
        /// The Observer/Observed Pattern is great for keeping the UI in Sync with the actual Values.
        /// Each list below represents a single Observed class that gets updated by other parts of the code, and will
        /// trigger the list of Observers that are looking for changes in that class.
        ///
        /// The list is serialized, so you can navigate to the Observers via the Inspector to see who's watching.
        /// </summary>
        [SerializeField]
        List<LocalMenuStateObserver> m_LocalMenuStateObservers = new List<LocalMenuStateObserver>();
        [SerializeField]
        List<LocalLobbyObserver> m_LocalLobbyObservers = new List<LocalLobbyObserver>();
        [SerializeField]
        List<LobbyUserObserver> m_LocalUserObservers = new List<LobbyUserObserver>();
        [SerializeField]
        List<LobbyServiceDataObserver> m_LobbyServiceObservers = new List<LobbyServiceDataObserver>();

        #endregion

        public LobbyManager LobbyManager { get; private set; }
        LocalMenuState m_LocalMenuState = new LocalMenuState();
        LobbyUser m_LocalUser;
        LocalLobby m_LocalLobby;
        LobbyServiceData m_LobbyServiceData = new LobbyServiceData();
        LobbyUpdater m_LobbyUpdater;

        RelayUtpSetup m_RelaySetup;
        RelayUtpClient m_RelayClient;

        vivox.VivoxSetup m_VivoxSetup = new vivox.VivoxSetup();
        [SerializeField]
        List<vivox.VivoxUserHandler> m_vivoxUserHandlers;

        LobbyColor m_lobbyColorFilter;


        static GameManager m_GameManagerInstance;

        public static GameManager Instance
        {
            get
            {
                if (m_GameManagerInstance != null)
                    return m_GameManagerInstance;
                m_GameManagerInstance = FindObjectOfType<GameManager>();
                return m_GameManagerInstance;
            }
        }

        /// <summary>Rather than a setter, this is usable in-editor. It won't accept an enum, however.</summary>
        public void SetLobbyColorFilter(int color)
        {
            m_lobbyColorFilter = (LobbyColor)color;
        }

        /// <summary>
        /// The Messaging System handles most of the core Lobby Service calls, and catches the callbacks from those calls.
        /// These In turn update the observed variables and propagates the events to the game.
        /// When looking for the interactions, look up the MessageType and search for it in the code to see where it is used outside this script.
        /// EG. Locator.Get.Messenger.OnReceiveMessage(MessageType.RenameRequest, name);
        /// </summary>
        public async void OnReceiveMessage(MessageType type, object msg)
        {
            if (type == MessageType.CreateLobbyRequest)
            {
                LocalLobby.LobbyData createLobbyData = (LocalLobby.LobbyData)msg;
                var lobby = await LobbyManager.CreateLobbyAsync(
                    createLobbyData.LobbyName,
                    createLobbyData.MaxPlayerCount,
                    createLobbyData.Private, m_LocalUser);

                if (lobby != null)
                {
                    LobbyConverters.RemoteToLocal(lobby, m_LocalLobby);
                    OnCreatedLobby();
                }
                else
                {
                    OnFailedJoin();
                }

            }
            else if (type == MessageType.JoinLobbyRequest)
            {
                LocalLobby.LobbyData lobbyInfo = (LocalLobby.LobbyData)msg;
                var lobby = await LobbyManager.JoinLobbyAsync(lobbyInfo.LobbyID, lobbyInfo.LobbyCode,
                    m_LocalUser);
                if (lobby != null)
                {
                    LobbyConverters.RemoteToLocal(lobby, m_LocalLobby);
                    OnJoinedLobby();
                }
                else
                {
                    OnFailedJoin();
                }

            }
            else if (type == MessageType.QueryLobbies)
            {
                m_LobbyServiceData.State = LobbyQueryState.Fetching;
                var qr = await LobbyManager.RetrieveLobbyListAsync(m_lobbyColorFilter);

                if (qr != null)
                    OnLobbiesQueried(LobbyConverters.QueryToLocalList(qr));
                else
                    OnLobbyQueryFailed();

            }
            else if (type == MessageType.QuickJoin)
            {
                var lobby = await LobbyManager.QuickJoinLobbyAsync(m_LocalUser, m_lobbyColorFilter);
                if (lobby != null)
                {
                    LobbyConverters.RemoteToLocal(lobby, m_LocalLobby);
                    OnJoinedLobby();
                }
                else
                {
                    OnFailedJoin();
                }
            }
            else if (type == MessageType.RenameRequest)
            {
                string name = (string)msg;
                if (string.IsNullOrWhiteSpace(name))
                {
                    Locator.Get.Messenger.OnReceiveMessage(MessageType.DisplayErrorPopup, "Empty Name not allowed."); // Lobby error type, then HTTP error type.
                    return;
                }

                m_LocalUser.DisplayName = (string)msg;
            }
            else if (type == MessageType.ClientUserApproved)
            {
                ConfirmApproval();
            }
            else if (type == MessageType.UserSetEmote)
            {
                EmoteType emote = (EmoteType)msg;
                m_LocalUser.Emote = emote;
            }
            else if (type == MessageType.LobbyUserStatus)
            {
                m_LocalUser.UserStatus = (UserStatus)msg;
            }
            else if (type == MessageType.StartCountdown)
            {
                m_LocalLobby.State = LobbyState.CountDown;
            }
            else if (type == MessageType.CancelCountdown)
            {
                m_LocalLobby.State = LobbyState.Lobby;
            }
            else if (type == MessageType.CompleteCountdown)
            {
                if (m_RelayClient is RelayUtpHost)
                    (m_RelayClient as RelayUtpHost).SendInGameState();
            }
            else if (type == MessageType.ChangeMenuState)
            {
                SetGameState((GameState)msg);
            }
            else if (type == MessageType.ConfirmInGameState)
            {
                m_LocalUser.UserStatus = UserStatus.InGame;
                m_LocalLobby.State = LobbyState.InGame;
            }
            else if (type == MessageType.EndGame)
            {
                m_LocalLobby.State = LobbyState.Lobby;
                SetUserLobbyState();
            }
        }


            #region Setup

        async void Awake()
        {
            // Do some arbitrary operations to instantiate singletons.
#pragma warning disable IDE0059 // Unnecessary assignment of a value
            var unused = Locator.Get;
#pragma warning restore IDE0059

            Application.wantsToQuit += OnWantToQuit;
            await InitializeServices();
            InitializeLobbies();
            StartVivoxLogin();
            Locator.Get.Messenger.Subscribe(this);
            BeginObservers();
        }

        async Task InitializeServices()
        {
            string serviceProfileName = "player";
#if UNITY_EDITOR
            serviceProfileName = $"{serviceProfileName}_{ClonesManager.GetCurrentProject().name}";
#endif
            await Auth.Authenticate(serviceProfileName);
        }

        void InitializeLobbies()
        {
            m_LocalLobby = new LocalLobby {State = LobbyState.Lobby};
            m_LocalUser = new LobbyUser();
            m_LocalUser.ID = AuthenticationService.Instance.PlayerId;
            m_LocalUser.DisplayName = NameGenerator.GetName(m_LocalUser.ID);
            m_LocalLobby
                .AddPlayer(m_LocalUser); // The local LobbyUser object will be hooked into UI before the LocalLobby is populated during lobby join, so the LocalLobby must know about it already when that happens.
            LobbyManager = new LobbyManager();
            m_LobbyUpdater = new LobbyUpdater(LobbyManager);
        }

        /// <summary>
        /// TODO Wire is a good update to remove the monolithic observers and move to observed values instead, on a Singleton gameManager
        /// </summary>
         void BeginObservers()
        {
            foreach (var gameStateObs in m_LocalMenuStateObservers)
                gameStateObs.BeginObserving(m_LocalMenuState);
            foreach (var serviceObs in m_LobbyServiceObservers)
                serviceObs.BeginObserving(m_LobbyServiceData);
            foreach (var lobbyObs in m_LocalLobbyObservers)
                lobbyObs.BeginObserving(m_LocalLobby);
            foreach (var userObs in m_LocalUserObservers)
                userObs.BeginObserving(m_LocalUser);
        }

        #endregion

        void SetGameState(GameState state)
        {
            bool isLeavingLobby = (state == GameState.Menu || state == GameState.JoinMenu) && m_LocalMenuState.State == GameState.Lobby;
            m_LocalMenuState.State = state;
            if (isLeavingLobby)
                OnLeftLobby();
        }

        void OnLobbiesQueried(IEnumerable<LocalLobby> lobbies)
        {
            var newLobbyDict = new Dictionary<string, LocalLobby>();
            foreach (var lobby in lobbies)
                newLobbyDict.Add(lobby.LobbyID, lobby);

            m_LobbyServiceData.State = LobbyQueryState.Fetched;
            m_LobbyServiceData.CurrentLobbies = newLobbyDict;
        }

        void OnLobbyQueryFailed()
        {
            m_LobbyServiceData.State = LobbyQueryState.Error;
        }

        void OnCreatedLobby()
        {
            m_LocalUser.IsHost = true;
            OnJoinedLobby();
        }

        void OnJoinedLobby()
        {
            m_LobbyUpdater.BeginTracking(m_LocalLobby, m_LocalUser);
            SetUserLobbyState();

            // The host has the opportunity to reject incoming players, but to do so the player needs to connect to Relay without having game logic available.
            // In particular, we should prevent players from joining voice chat until they are approved.
            OnReceiveMessage(MessageType.LobbyUserStatus, UserStatus.Lobby);
            if (m_LocalUser.IsHost)
            {
               // StartRelayConnection();
                StartVivoxJoin();
            }
            else
            {
               // StartRelayConnection();
            }
        }

        void OnLeftLobby()
        {
            m_LocalUser.ResetState();
#pragma warning disable 4014
            LobbyManager.LeaveLobbyAsync(m_LocalLobby.LobbyID);
#pragma warning restore 4014
            ResetLocalLobby();
            m_LobbyUpdater.EndTracking();
            m_VivoxSetup.LeaveLobbyChannel();

            if (m_RelaySetup != null)
            {
                Component.Destroy(m_RelaySetup);
                m_RelaySetup = null;
            }

            if (m_RelayClient != null)
            {
                m_RelayClient.Dispose();
                StartCoroutine(FinishCleanup());

                // We need to delay slightly to give the disconnect message sent during Dispose time to reach the host, so that we don't destroy the connection without it being flushed first.
                IEnumerator FinishCleanup()
                {
                    yield return null;
                    Component.Destroy(m_RelayClient);
                    m_RelayClient = null;
                }
            }
        }

        /// <summary>
        /// Back to Join menu if we fail to join for whatever reason.
        /// </summary>
        void OnFailedJoin()
        {
            SetGameState(GameState.JoinMenu);
        }

        void StartVivoxLogin()
        {
            m_VivoxSetup.Initialize(m_vivoxUserHandlers, OnVivoxLoginComplete);

            void OnVivoxLoginComplete(bool didSucceed)
            {
                if (!didSucceed)
                {
                    Debug.LogError("Vivox login failed! Retrying in 5s...");
                    StartCoroutine(RetryConnection(StartVivoxLogin, m_LocalLobby.LobbyID));
                }
            }
        }

        void StartVivoxJoin()
        {
            m_VivoxSetup.JoinLobbyChannel(m_LocalLobby.LobbyID, OnVivoxJoinComplete);

            void OnVivoxJoinComplete(bool didSucceed)
            {
                if (!didSucceed)
                {
                    Debug.LogError("Vivox connection failed! Retrying in 5s...");
                    StartCoroutine(RetryConnection(StartVivoxJoin, m_LocalLobby.LobbyID));
                }
            }
        }

        void StartRelayConnection()
        {
            if (m_LocalUser.IsHost)
                m_RelaySetup = gameObject.AddComponent<RelayUtpSetupHost>();
            else
                m_RelaySetup = gameObject.AddComponent<RelayUtpSetupClient>();
            m_RelaySetup.BeginRelayJoin(m_LocalLobby, m_LocalUser, OnRelayConnected);

            void OnRelayConnected(bool didSucceed, RelayUtpClient client)
            {
                Component.Destroy(m_RelaySetup);
                m_RelaySetup = null;

                if (!didSucceed)
                {
                    Debug.LogError("Relay connection failed! Retrying in 5s...");
                    StartCoroutine(RetryConnection(StartRelayConnection, m_LocalLobby.LobbyID));
                    return;
                }

                m_RelayClient = client;
                if (m_LocalUser.IsHost)
                    CompleteRelayConnection();
                else
                    Debug.Log("Client is now waiting for approval...");
            }
        }

        IEnumerator RetryConnection(Action doConnection, string lobbyId)
        {
            yield return new WaitForSeconds(5);
            if (m_LocalLobby != null && m_LocalLobby.LobbyID == lobbyId && !string.IsNullOrEmpty(lobbyId)) // Ensure we didn't leave the lobby during this waiting period.
                doConnection?.Invoke();
        }

        void ConfirmApproval()
        {
            if (!m_LocalUser.IsHost && m_LocalUser.IsApproved)
            {
                CompleteRelayConnection();
                StartVivoxJoin();
            }
        }

        void CompleteRelayConnection()
        {
            OnReceiveMessage(MessageType.LobbyUserStatus, UserStatus.Lobby);
        }

        void SetUserLobbyState()
        {
            SetGameState(GameState.Lobby);
            OnReceiveMessage(MessageType.LobbyUserStatus, UserStatus.Lobby);
        }

        void ResetLocalLobby()
        {
            m_LocalLobby.CopyObserved(new LocalLobby.LobbyData(), new Dictionary<string, LobbyUser>());
            m_LocalLobby.AddPlayer(m_LocalUser); // As before, the local player will need to be plugged into UI before the lobby join actually happens.
            m_LocalLobby.RelayServer = null;
        }

        #region Teardown

        /// <summary>
        /// In builds, if we are in a lobby and try to send a Leave request on application quit, it won't go through if we're quitting on the same frame.
        /// So, we need to delay just briefly to let the request happen (though we don't need to wait for the result).
        /// </summary>
        IEnumerator LeaveBeforeQuit()
        {
            ForceLeaveAttempt();
            yield return null;
            Application.Quit();
        }

        bool OnWantToQuit()
        {
            bool canQuit = string.IsNullOrEmpty(m_LocalLobby?.LobbyID);
            StartCoroutine(LeaveBeforeQuit());
            return canQuit;
        }

        void OnDestroy()
        {
            ForceLeaveAttempt();
            m_LobbyUpdater.Dispose();
            LobbyManager.Dispose();
        }

        void ForceLeaveAttempt()
        {
            Locator.Get.Messenger.Unsubscribe(this);
            if (!string.IsNullOrEmpty(m_LocalLobby?.LobbyID))
            {
#pragma warning disable 4014
                LobbyManager.LeaveLobbyAsync(m_LocalLobby?.LobbyID);
#pragma warning restore 4014
                m_LocalLobby = null;
            }
        }

        #endregion
    }
}
