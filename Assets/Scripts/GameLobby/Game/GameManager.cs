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
    public class GameManager : MonoBehaviour
    {

        public LocalLobby LocalLobby => m_LocalLobby;
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


        public async Task<LocalPlayer> AwaitLocalUserInitialization()
        {
            while (m_LocalUser == null)
                await Task.Delay(100);
            return m_LocalUser;
        }

        public async void CreateLobby(string name, bool isPrivate, int maxPlayers = 4)
        {
            var lobby = await LobbyManager.CreateLobbyAsync(
                name,
                maxPlayers,
                isPrivate, m_LocalUser);

            if (lobby != null)
            {
                try
                {
                    LobbyConverters.RemoteToLocal(lobby, m_LocalLobby);
                }
                catch (Exception exception)
                {
                    Debug.LogError(exception);
                }
                CreateLobby();
            }
            else
            {
                SetGameState(GameState.JoinMenu);
            }
        }


        public async void JoinLobby(LocalLobby lobbyInfo)
        {
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


        public async void QueryLobbies()
        {
            LobbyList.QueryState.Value = LobbyQueryState.Fetching;
            var qr = await LobbyManager.RetrieveLobbyListAsync(m_lobbyColorFilter);

            if (qr != null)
                SetCurrentLobbies(LobbyConverters.QueryToLocalList(qr));
            else
                LobbyList.QueryState.Value = LobbyQueryState.Error;
        }


        public async void QuickJoin()
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

        public void SetLocalUserName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                LogHandlerSettings.Instance.SpawnErrorPopup(
                    "Empty Name not allowed."); // Lobby error type, then HTTP error type.
                return;
            }

            m_LocalUser.DisplayName.Value = name;
        }

        public void SetLocalUserEmote(EmoteType emote)
        {
            SetUserEmote(m_LocalUser.ID.Value, emote);
        }

        public void SetUserEmote(string playerID, EmoteType emote)
        {
            var player = GetPlayerByID(playerID);
            if (player == null)
                return;
            player.Emote.Value = emote;

        }
        public void SetLocalUserStatus(UserStatus status)
        {
            SetUserStatus(m_LocalUser.ID.Value, status);
        }
        public void SetUserStatus(string playerID, UserStatus status)
        {
            var player = GetPlayerByID(playerID);
            if (player == null)
                return;
            m_LocalUser.UserStatus.Value = status;
        }

        public void CompleteCountDown()
        {
            Debug.Log("CountDown Complete!");
        }

        public void ChangeMenuState(GameState state)
        {
            SetGameState(state);
        }

        public void ConfirmIngameState()
        {
            m_setupInGame.ConfirmInGameState();
        }

        public void BeginGame()
        {
            if (m_LocalUser.IsHost.Value)
                m_LocalLobby.Locked.Value = true;
            m_LocalLobby.LocalLobbyState.Value = LobbyState.InGame;
            m_setupInGame?.MiniGameBeginning();
        }

        public void EndGame()
        {
            m_LocalLobby.LocalLobbyState.Value = LobbyState.Lobby;
            m_setupInGame?.OnGameEnd();
            LocalUserToLobby();
        }

        public void BeginCountdown()
        {
            Debug.Log("Beginning Countdown.");
            m_LocalLobby.LocalLobbyState.Value = LobbyState.CountDown;
            m_countdown.StartCountDown();
        }

        public void CancelCountDown()
        {
            Debug.Log("Countdown Cancelled.");
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
            m_LocalUser = new LocalPlayer("", false, "LocalPlayer");
            m_LocalLobby = new LocalLobby { LocalLobbyState = { Value = LobbyState.Lobby } };
            await InitializeServices();
            AuthenticatePlayer();
            StartVivoxLogin();
        }

        async Task InitializeServices()
        {
            string serviceProfileName = "player";
#if UNITY_EDITOR
            serviceProfileName = $"{serviceProfileName}_{ClonesManager.GetCurrentProject().name}";
#endif
            await Auth.Authenticate(serviceProfileName);
        }

        void AuthenticatePlayer()
        {
            var localId = AuthenticationService.Instance.PlayerId;
            var randomName = NameGenerator.GetName(localId);

            m_LocalUser.ID.Value = localId;
            m_LocalUser.DisplayName.Value = randomName;

            m_LocalLobby.AddPlayer(m_LocalUser); // The local LocalPlayer object will be hooked into UI
        }

        #endregion

        LocalPlayer GetPlayerByID(string playerID)
        {
            if (m_LocalUser.ID.Value == playerID)
                return m_LocalUser;

            if (!m_LocalLobby.LocalPlayers.ContainsKey(playerID))
            {
                Debug.LogError($"No player by id : {playerID} in Local Lobby");
                return null;
            }
            return m_LocalLobby.LocalPlayers[playerID];
        }

        void SetGameState(GameState state)
        {
            bool isLeavingLobby = (state == GameState.Menu || state == GameState.JoinMenu) &&
                                  LocalGameState == GameState.Lobby;
            LocalGameState = state;
            Debug.Log($"Switching Game State to : {LocalGameState}");
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
            LocalUserToLobby();
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
                SetUserStatus(m_LocalUser.ID.Value, UserStatus.Lobby);
            }
        }

        IEnumerator RetryConnection(Action doConnection, string lobbyId)
        {
            yield return new WaitForSeconds(5);
            if (m_LocalLobby != null && m_LocalLobby.LobbyID.Value == lobbyId && !string.IsNullOrEmpty(lobbyId)
            ) // Ensure we didn't leave the lobby during this waiting period.
                doConnection?.Invoke();
        }

        void LocalUserToLobby()
        {
            Debug.Log($"Setting Lobby user state {GameState.Lobby}");
            SetGameState(GameState.Lobby);
            SetUserStatus(m_LocalUser.ID.Value, UserStatus.Lobby);
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
