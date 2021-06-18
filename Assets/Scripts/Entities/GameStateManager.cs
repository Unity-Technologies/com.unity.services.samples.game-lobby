using System.Collections;
using System;
using System.Collections.Generic;
using LobbyRooms.Relay;
using Player;
using Unity.Services.Relay.Models;
using UnityEngine;
using Utilities;

namespace LobbyRooms
{
    public class GameStateManager : MonoBehaviour, IReceiveMessages
    {
        /// <summary>
        /// All these should be assigned the observers in the scene at the start.
        /// </summary>
        [SerializeField]
        List<LocalGameStateObserver> m_LocalGameStateObservers = new List<LocalGameStateObserver>();
        [SerializeField]
        List<LobbyDataObserver> m_LobbyDataObservers = new List<LobbyDataObserver>();
        [SerializeField]
        List<LobbyUserObserver> m_LocalUserObservers = new List<LobbyUserObserver>();
        [SerializeField]
        List<LobbyServiceDataObserver> m_LobbyServiceObservers = new List<LobbyServiceDataObserver>();

        private RoomsContentHeartbeat m_roomsContentHeartbeat = new RoomsContentHeartbeat();

        LocalGameState m_localGameState = new LocalGameState();
        LobbyUser m_localUser;
        LobbyData m_lobbyData;
        LobbyServiceData m_lobbyServiceData = new LobbyServiceData();

        LobbyReadyCheck m_LobbyReadyCheck = null;

        public void Awake()
        {
            // Do some arbitrary operations to instantiate singletons.
            LogHandler.Get();
#pragma warning disable IDE0059 // Unnecessary assignment of a value
            var unused = Locator.Get;
#pragma warning restore IDE0059 // Unnecessary assignment of a value
            Locator.Get.Provide(new Auth.Identity(OnAuthSignIn));
        }

        private void OnAuthSignIn()
        {
            Debug.Log("Signed in.");
            m_localUser.ID = Locator.Get.Identity.GetSubIdentity(Auth.IIdentityType.Auth).GetContent("id");
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
                RoomsQuery.Instance.CreateRoomAsync(createRoomData.LobbyName, createRoomData.MaxPlayerCount, (r) => { Rooms.ToLobbyData.Convert(r, m_lobbyData, m_localUser); OnCreatedRoom(); }, null); // TODO: Report failure?
            }
            else if (type == MessageType.JoinRoomRequest)
            {
                LobbyInfo roomData = (LobbyInfo)msg;
                RoomsQuery.Instance.JoinRoomAsync(roomData.RoomID, roomData.RoomCode, (r) => { Rooms.ToLobbyData.Convert(r, m_lobbyData, m_localUser); OnJoinedRoom(); }, null); // TODO: Report failure?
            }
            else if (type == MessageType.QueryRooms)
            {
                RoomsQuery.Instance.RetrieveRoomListAsync((qr) => { OnRefreshed(Rooms.ToLobbyData.Convert(qr)); });
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
            else if (type == MessageType.HostInitReadyCheck) //A message that either Initiates or Cancels a ready check. Inititated either by the local user, or recieved via the Room service.
            {
                var notCancelledByHost = (bool)msg;
                m_LobbyReadyCheck = new LobbyReadyCheck(m_lobbyData, OnReadyComplete);
                if (notCancelledByHost)
                {
                    if (m_localUser.IsHost)
                    {
                        m_localUser.UserStatus = UserStatus.Ready;
                    }
                    else
                    {
                        m_localUser.UserStatus = UserStatus.ReadyCheck;
                    }
                }
                else
                {
                    m_localUser.UserStatus = UserStatus.Cancelled;
                    m_LobbyReadyCheck.Dispose();
                }
            }
            else if (type == MessageType.LocalUserReadyCheckResponse)
            {
                var isReady = (bool)msg;
                if (isReady)
                    m_localUser.UserStatus = UserStatus.Ready;
                else
                    m_localUser.UserStatus = UserStatus.Cancelled;
            }
            else if (type == MessageType.ChangeLobbyUserState)
            {
                UserStatus newStatus = (UserStatus)msg;

                m_localUser.UserStatus = newStatus;
            }
        }

        void Start()
        {
            m_lobbyData = new LobbyData();
            m_localUser = new LobbyUser();
            Locator.Get.Messenger.Subscribe(this);
            InitObservers();
        }

        void InitObservers()
        {
            foreach (var gameStateObs in m_LocalGameStateObservers)
            {
                gameStateObs.BeginObserving(m_localGameState);
            }

            foreach (var serviceObs in m_LobbyServiceObservers)
            {
                serviceObs.BeginObserving(m_lobbyServiceData);
            }

            foreach (var lobbyObs in m_LobbyDataObservers)
            {
                lobbyObs.BeginObserving(m_lobbyData);
            }

            foreach (var userObs in m_LocalUserObservers)
            {
                userObs.BeginObserving(m_localUser);
            }
        }

        void SetGameState(GameState state)
        {
            m_localGameState.State = state;
            if (state == GameState.Menu)
            {
                m_localUser.Emote = null; // TODO: Should we have a more centralized location for cleaning up on room leave? We still have UI bugs with, e.g., leaving and rejoining rooms.
                RoomsQuery.Instance.LeaveRoomAsync(m_lobbyData.RoomID, () => { m_lobbyData.CopyObserved(new LobbyInfo(), new Dictionary<string, LobbyUser>()); });
            }
        }

        void OnReadyComplete(bool success)
        {
            if (success)
            {
                m_localGameState.State = GameState.Joining;
                RelayInterface.JoinAsync(m_lobbyData.RelayCode, OnJoined);
            }
            else
            {
                m_localGameState.State = GameState.Lobby;
            }
        }

        void OnJoined(JoinAllocation joinData)
        {
            var ip = joinData.RelayServer.IpV4;
            var port = joinData.RelayServer.Port;
            m_lobbyData.RelayServer = new ServerAddress(ip, port);
        }

        void OnRefreshed(IEnumerable<LobbyData> lobbies)
        {
            var newLobbyDict = new Dictionary<string, LobbyData>();
            foreach (var lobby in lobbies)
            {
                newLobbyDict.Add(lobby.RoomID, lobby);
            }

            m_lobbyServiceData.CurrentLobbies = newLobbyDict;
        }

        void OnCreatedRoom()
        {
            OnJoinedRoom();
            RelayInterface.AllocateAsync(m_lobbyData.MaxPlayerCount, OnGotRelayAllocation);
        }

        void OnGotRelayAllocation(Allocation allocationID)
        {
            Debug.Log("allocated relay server");
            RelayInterface.GetJoinCodeAsync(allocationID.AllocationId, OnGotRelayCode);
        }

        void OnGotRelayCode(string relayCode)
        {
            Debug.Log($"Got Relay code: {relayCode}");
            m_lobbyData.RelayCode = relayCode;
        }

        void OnJoinedRoom()
        {
            SetGameState(GameState.Lobby);
            m_localUser.UserStatus = UserStatus.Lobby;
            m_roomsContentHeartbeat.BeginTracking(m_lobbyData, m_localUser); // TODO: End tracking somewhere

            Dictionary<string, string> displayNameData = new Dictionary<string, string>();
            displayNameData.Add("DisplayName", m_localUser.DisplayName);
            RoomsQuery.Instance.RetrieveRoomAsync(m_lobbyData.RoomID, (r) => {
                RoomsQuery.Instance.UpdatePlayerDataAsync(r, m_localUser.ID, displayNameData, null);
            });
        }

        void OnDestroy()
        {
            Locator.Get.Messenger.Unsubscribe(this);
            if (!string.IsNullOrEmpty(m_lobbyData.RoomID))
                RoomsQuery.Instance.LeaveRoomAsync(m_lobbyData.RoomID, null);
        }
    }
}
