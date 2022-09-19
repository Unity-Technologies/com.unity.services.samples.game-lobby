using LobbyRelaySample.relay;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using LobbyRelaySample.lobby;
using LobbyRelaySample.ngo;
using Unity.Services.Authentication;
using UnityEngine;
#if UNITY_EDITOR
using ParrelSync;

#endif

namespace LobbyRelaySample
{
    /// <summary>
    /// Current state of the local game.
    /// Set as a flag to allow for the Inspector to select multiple valid states for various UI features.
    /// </summary>
    [Flags]
    public enum GameState
    {
        Menu = 1,
        Lobby = 2,
        JoinMenu = 4,
    }

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
        List<LocalLobbyObserver> m_LocalLobbyObservers = new List<LocalLobbyObserver>();
        [SerializeField]
        List<LobbyUserObserver> m_LocalUserObservers = new List<LobbyUserObserver>();

        #endregion

        public LocalLobby LocalLobby => m_LocalLobby;
        public LocalPlayer LocalUser => m_LocalUser;
        public Action<GameState> onGameStateChanged;
        public LocalLobbyList LobbyList { get; private set; } = new LocalLobbyList();

        public GameState LocalGameState { get; private set; }
        public LobbyManager LobbyManager { get; private set; }
        [SerializeField]
        SetupInGame m_setupInGame;
        [SerializeField]
        Countdown m_countdown;

        LocalPlayer m_LocalUser;
        LocalLobby m_LocalLobby;
        LobbySynchronizer m_LobbySynchronizer;

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

        public async Task CreateLobby(string name, bool isPrivate, int maxPlayers = 4)
        {
            var lobby = await LobbyManager.CreateLobbyAsync(
                name,
                maxPlayers,
                isPrivate, m_LocalUser);

            if (lobby != null)
            {
                LobbyConverters.RemoteToLocal(lobby, m_LocalLobby);
                CreateLobby();
            }
            else
            {
                SetGameState(GameState.JoinMenu);
            }
        }

        /// <summary>
        /// The Messaging System handles most of the core Lobby Service calls, and catches the callbacks from those calls.
        /// These In turn update the observed variables and propagates the events to the game.
        /// When looking for the interactions, look up the MessageType and search for it in the code to see where it is used outside this script.
        /// EG. Locator.Get.Messenger.OnReceiveMessage(MessageType.RenameRequest, name);
        /// </summary>
        public async void OnReceiveMessage(MessageType type, object msg)
        {
            if (type == MessageType.JoinLobbyRequest)
            {
                LocalLobby lobbyInfo = (LocalLobby)msg;
                var lobby = await LobbyManager.JoinLobbyAsync(lobbyInfo.LobbyID.Value, lobbyInfo.LobbyCode.Value,
                    m_LocalUser);
                if (lobby != null)
                {
                    LobbyConverters.RemoteToLocal(lobby, m_LocalLobby);
                    JoinLobby();
                }
                else
                {
                    SetGameState(GameState.JoinMenu);
                }
            }
            else if (type == MessageType.QueryLobbies)
            {
                LobbyList.QueryState.Value = LobbyQueryState.Fetching;
                var qr = await LobbyManager.RetrieveLobbyListAsync(m_lobbyColorFilter);

                if (qr != null)
                    SetCurrentLobbies(LobbyConverters.QueryToLocalList(qr));
                else
                    LobbyList.QueryState.Value = LobbyQueryState.Error;
            }
            else if (type == MessageType.QuickJoin)
            {
                var lobby = await LobbyManager.QuickJoinLobbyAsync(m_LocalUser, m_lobbyColorFilter);
                if (lobby != null)
                {
                    LobbyConverters.RemoteToLocal(lobby, m_LocalLobby);
                    JoinLobby();
                }
                else
                {
                    SetGameState(GameState.JoinMenu);
                }
            }
            else if (type == MessageType.RenameRequest)
            {
                string name = (string)msg;
                if (string.IsNullOrWhiteSpace(name))
                {
                    Locator.Get.Messenger.OnReceiveMessage(MessageType.DisplayErrorPopup,
                        "Empty Name not allowed."); // Lobby error type, then HTTP error type.
                    return;
                }

                m_LocalUser.DisplayName.Value = (string)msg;
            }
            else if (type == MessageType.UserSetEmote)
            {
                EmoteType emote = (EmoteType)msg;
                m_LocalUser.Emote.Value = emote;
            }
            else if (type == MessageType.LobbyUserStatus)
            {
                m_LocalUser.UserStatus.Value = (UserStatus)msg;
            }
            else if (type == MessageType.CompleteCountdown)
            {
                //Start game for everyone
                if (m_RelayClient is RelayUtpHost)
                    (m_RelayClient as RelayUtpHost).SendInGameState();
            }
            else if (type == MessageType.ChangeMenuState)
            {
                SetGameState((GameState)msg);
            }
            else if (type == MessageType.ConfirmInGameState) { }
            else if (type == MessageType.EndGame)
            {
                m_LocalLobby.LocalLobbyState.Value = LobbyState.Lobby;
                SetUserLobbyState();
            }
        }

        public void BeginCountdown()
        {
            m_LocalLobby.LocalLobbyState.Value = LobbyState.CountDown;
            m_countdown.StartCountDown();
        }

        public void CancelCountDown()
        {
            m_countdown.CancelCountDown();
            m_LocalLobby.LocalLobbyState.Value = LobbyState.Lobby;
        }

        public void FinishedCountDown()
        {
            m_LocalUser.UserStatus.Value = UserStatus.InGame;
            m_LocalLobby.LocalLobbyState.Value = LobbyState.InGame;
            m_setupInGame.StartNetworkedGame(m_LocalLobby, m_LocalUser);
        }

        #region Setup

        async void Awake()
        {
            // Do some arbitrary operations to instantiate singletons.
#pragma warning disable IDE0059 // Unnecessary assignment of a value
            var unused = Locator.Get;
#pragma warning restore IDE0059

            Application.wantsToQuit += OnWantToQuit;
            LobbyManager = new LobbyManager();
            m_LobbySynchronizer = new LobbySynchronizer(LobbyManager);
            InitLocalInstances();
            await InitializeServices();
            InitLocalPlayerId();
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

        void InitLocalInstances()
        {
            m_LocalLobby = new LocalLobby { LocalLobbyState = { Value = LobbyState.Lobby } };
        }

        void InitLocalPlayerId()
        {
            var localId = AuthenticationService.Instance.PlayerId;
            var randomName = NameGenerator.GetName(localId);

            m_LocalUser = new LocalPlayer(localId, false, randomName);

            m_LocalLobby.AddPlayer(m_LocalUser); // The local LocalPlayer object will be hooked into UI
        }

        void BeginObservers()
        {
            foreach (var lobbyObs in m_LocalLobbyObservers)
                lobbyObs.BeginObserving(m_LocalLobby);
            foreach (var userObs in m_LocalUserObservers)
                userObs.BeginObserving(m_LocalUser);
        }

        #endregion

        void SetGameState(GameState state)
        {
            bool isLeavingLobby = (state == GameState.Menu || state == GameState.JoinMenu) &&
                LocalGameState == GameState.Lobby;
            LocalGameState = state;
            if (isLeavingLobby)
                LeaveLobby();
            onGameStateChanged.Invoke(LocalGameState);
        }

        void SetCurrentLobbies(IEnumerable<LocalLobby> lobbies)
        {
            var newLobbyDict = new Dictionary<string, LocalLobby>();
            foreach (var lobby in lobbies)
                newLobbyDict.Add(lobby.LobbyID.Value, lobby);

            LobbyList.QueryState.Value = LobbyQueryState.Fetched;
            LobbyList.CurrentLobbies = newLobbyDict;
        }

        void CreateLobby()
        {
            m_LocalUser.IsHost.Value = true;
            JoinLobby();
        }

        void JoinLobby()
        {
            m_LobbySynchronizer.StartSynch(m_LocalLobby, m_LocalUser);
            SetUserLobbyState();
            StartVivoxJoin();
        }

        void LeaveLobby()
        {
            m_LocalUser.ResetState();
#pragma warning disable 4014
            LobbyManager.LeaveLobbyAsync();
#pragma warning restore 4014
            ResetLocalLobby();
            m_LobbySynchronizer.EndSynch();
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

        void StartVivoxLogin()
        {
            m_VivoxSetup.Initialize(m_vivoxUserHandlers, OnVivoxLoginComplete);

            void OnVivoxLoginComplete(bool didSucceed)
            {
                if (!didSucceed)
                {
                    Debug.LogError("Vivox login failed! Retrying in 5s...");
                    StartCoroutine(RetryConnection(StartVivoxLogin, m_LocalLobby.LobbyID.Value));
                }
            }
        }

        void StartVivoxJoin()
        {
            m_VivoxSetup.JoinLobbyChannel(m_LocalLobby.LobbyID.Value, OnVivoxJoinComplete);

            void OnVivoxJoinComplete(bool didSucceed)
            {
                if (!didSucceed)
                {
                    Debug.LogError("Vivox connection failed! Retrying in 5s...");
                    StartCoroutine(RetryConnection(StartVivoxJoin, m_LocalLobby.LobbyID.Value));
                }
            }
        }

        void StartRelayConnection()
        {
            if (m_LocalUser.IsHost.Value)
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
                    StartCoroutine(RetryConnection(StartRelayConnection, m_LocalLobby.LobbyID.Value));
                    return;
                }

                m_RelayClient = client;
                OnReceiveMessage(MessageType.LobbyUserStatus, UserStatus.Lobby);
            }
        }

        IEnumerator RetryConnection(Action doConnection, string lobbyId)
        {
            yield return new WaitForSeconds(5);
            if (m_LocalLobby != null && m_LocalLobby.LobbyID.Value == lobbyId && !string.IsNullOrEmpty(lobbyId)
            ) // Ensure we didn't leave the lobby during this waiting period.
                doConnection?.Invoke();
        }

        void SetUserLobbyState()
        {
            SetGameState(GameState.Lobby);
            OnReceiveMessage(MessageType.LobbyUserStatus, UserStatus.Lobby);
        }

        void ResetLocalLobby()
        {
            m_LocalLobby.ResetLobby();
            m_LocalLobby
                .AddPlayer(m_LocalUser); // As before, the local player will need to be plugged into UI before the lobby join actually happens.
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
            bool canQuit = string.IsNullOrEmpty(m_LocalLobby?.LobbyID.Value);
            StartCoroutine(LeaveBeforeQuit());
            return canQuit;
        }

        void OnDestroy()
        {
            ForceLeaveAttempt();
            m_LobbySynchronizer.Dispose();
            LobbyManager.Dispose();
        }

        void ForceLeaveAttempt()
        {
            Locator.Get.Messenger.Unsubscribe(this);
            if (!string.IsNullOrEmpty(m_LocalLobby?.LobbyID.Value))
            {
#pragma warning disable 4014
                LobbyManager.LeaveLobbyAsync();
#pragma warning restore 4014
                m_LocalLobby = null;
            }
        }

        #endregion
    }
}