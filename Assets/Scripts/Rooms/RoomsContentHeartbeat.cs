using Utilities;

namespace LobbyRooms
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
            lobby.onChanged += OnLobbyDataChanged;
        }

        private void OnLobbyDataChanged(LobbyData changed)
        {
            if (string.IsNullOrEmpty(changed.RoomID)) // When the player leaves, their LobbyData is cleared out but maintained.
            {
                m_shouldPushData = false;
                Locator.Get.UpdateSlow.Unsubscribe(OnUpdate);
                m_lobbyData.onChanged -= OnLobbyDataChanged;
                m_lobbyData = null;
                m_localUser = null;
            }
            m_shouldPushData = true;
        }

        public void OnUpdate(float dt)
        {
            if (m_isAwaitingQuery || m_lobbyData == null)
                return;

            m_isAwaitingQuery = true; // TODO: Recover if we fail? Try-catch?
            if (m_shouldPushData)
            {
                RoomsQuery.Instance.RetrieveRoomAsync(m_lobbyData.RoomID, (r) =>
                {
                    if (m_localUser.IsHost)
                        DoRoomDataPush(r);
                    else
                        DoPlayerDataPush(r);
                });
            }
            else
                RoomsQuery.Instance.RetrieveRoomAsync(m_lobbyData.RoomID, OnRetrieve);
            m_shouldPushData = false;

            void DoRoomDataPush(Unity.Services.Rooms.Models.Room room)
            {
                RoomsQuery.Instance.UpdateRoomDataAsync(room, Rooms.ToLobbyData.RetrieveRoomData(m_lobbyData), () => { DoPlayerDataPush(room); }); // TODO: This needs not happen as often as player updates, right?
            }

            void DoPlayerDataPush(Unity.Services.Rooms.Models.Room room)
            {
                RoomsQuery.Instance.UpdatePlayerDataAsync(room, m_localUser.ID, Rooms.ToLobbyData.RetrieveUserData(m_localUser), () => { m_isAwaitingQuery = false; });
            }

            void OnRetrieve(Unity.Services.Rooms.Models.Room room)
            {
                m_isAwaitingQuery = false;
                if (room != null)
                {
                    bool prevShouldPush = m_shouldPushData;
                    Rooms.ToLobbyData.Convert(room, m_lobbyData, m_localUser);
                    m_shouldPushData = prevShouldPush; // Copying the room data to the local lobby likely caused a change in its observed data, which would prompt updating room data, but that's not necessary here.
                }
            }
        }
    }
}
