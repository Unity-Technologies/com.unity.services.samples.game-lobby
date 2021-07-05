using System;
using Room = Unity.Services.Rooms.Models.Room;

namespace LobbyRelaySample
{
    /// <summary>
    /// Keep updated on changes to a joined room.
    /// FUTURE: Supposedly there will be an event-driven model soon, instead of having to poll a room?
    /// </summary>
    public class RoomsContentHeartbeat
    {
        private LobbyData m_lobbyData;
        private LobbyUser m_localUser;
        private bool m_isAwaitingQuery = false;
        private bool m_shouldPushData = false;

        public void BeginTracking(LobbyData lobby, LobbyUser localUser)
        {
            m_lobbyData = lobby;
            m_localUser = localUser;
            Locator.Get.UpdateSlow.Subscribe(OnUpdate);
            m_lobbyData.onChanged += OnLobbyDataChanged;
            m_shouldPushData = true; // Ensure the initial presence of a new player is pushed to the room; otherwise, when a non-host joins, the LobbyData never receives their data until they push something new.
        }

        public void EndTracking()
        {
            m_shouldPushData = false;
            Locator.Get.UpdateSlow.Unsubscribe(OnUpdate);
            if (m_lobbyData != null)
                m_lobbyData.onChanged -= OnLobbyDataChanged;
            m_lobbyData = null;
            m_localUser = null;
        }

        private void OnLobbyDataChanged(LobbyData changed)
        {
            if (string.IsNullOrEmpty(changed.RoomID)) // When the player leaves, their LobbyData is cleared out but maintained.
                EndTracking();
            m_shouldPushData = true;
        }

        public void OnUpdate(float dt)
        {
            if (m_isAwaitingQuery || m_lobbyData == null)
                return;

            m_isAwaitingQuery = true; // Note that because we make async calls, if one of them fails and doesn't call our callback, this will never be reset to false.
            if (m_shouldPushData)
                PushDataToRoom();
            else
                OnRetrieve();

            void PushDataToRoom()
            {
                if (m_localUser == null)
                {
                    m_isAwaitingQuery = false;
                    return; // Don't revert m_shouldPushData yet, so that we can retry.
                }
                m_shouldPushData = false;

                if (m_localUser.IsHost)
                    DoRoomDataPush();
                else
                    DoPlayerDataPush();
            }

            void DoRoomDataPush()
            {
                RoomsQuery.Instance.UpdateRoomDataAsync(Lobby.ToLobbyData.RetrieveRoomData(m_lobbyData), () => { DoPlayerDataPush(); });
            }

            void DoPlayerDataPush()
            {
                RoomsQuery.Instance.UpdatePlayerDataAsync(Lobby.ToLobbyData.RetrieveUserData(m_localUser), () => { m_isAwaitingQuery = false; });
            }

            void OnRetrieve()
            {
                m_isAwaitingQuery = false;
                Room room = RoomsQuery.Instance.CurrentRoom;
                if (room == null) return;
                bool prevShouldPush = m_shouldPushData;
                var prevState = m_lobbyData.State;
                Lobby.ToLobbyData.Convert(room, m_lobbyData, m_localUser);
                m_shouldPushData = prevShouldPush; // Copying the room data to the local lobby likely caused a change in its observed data, which would prompt updating room data, but that's not necessary here.
                CheckForRoomReady();

                if (prevState != LobbyState.Lobby && m_lobbyData.State == LobbyState.Lobby)
                    Locator.Get.Messenger.OnReceiveMessage(MessageType.ToLobby, null);
            }


            void CheckForRoomReady()
            {
                bool areAllPlayersReady = m_lobbyData.AllPlayersReadyTime != null;
                if (areAllPlayersReady)
                {
                    long targetTimeTicks = m_lobbyData.AllPlayersReadyTime.Value;
                    DateTime targetTime = new DateTime(targetTimeTicks);
                    if (targetTime.Subtract(DateTime.Now).Seconds < 0)
                        return;

                    Locator.Get.Messenger.OnReceiveMessage(MessageType.Client_EndReadyCountdownAt, targetTime); // Note that this could be called multiple times.
                }
            }
        }
    }
}
