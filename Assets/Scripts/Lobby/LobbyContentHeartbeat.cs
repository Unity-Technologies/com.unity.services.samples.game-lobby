using System;
using LobbyRemote = Unity.Services.Rooms.Models.Room;

namespace LobbyRelaySample
{
    /// <summary>
    /// Keep updated on changes to a joined lobby.
    /// </summary>
    public class LobbyContentHeartbeat
    {
        private LocalLobby m_localLobby;
        private LobbyUser m_localUser;
        private bool m_isAwaitingQuery = false;
        private bool m_shouldPushData = false;

        public void BeginTracking(LocalLobby lobby, LobbyUser localUser)
        {
            m_localLobby = lobby;
            m_localUser = localUser;
            Locator.Get.UpdateSlow.Subscribe(OnUpdate);
            m_localLobby.onChanged += OnLocalLobbyChanged;
            m_shouldPushData = true; // Ensure the initial presence of a new player is pushed to the lobby; otherwise, when a non-host joins, the LocalLobby never receives their data until they push something new.
        }

        public void EndTracking()
        {
            m_shouldPushData = false;
            Locator.Get.UpdateSlow.Unsubscribe(OnUpdate);
            if (m_localLobby != null)
                m_localLobby.onChanged -= OnLocalLobbyChanged;
            m_localLobby = null;
            m_localUser = null;
        }

        private void OnLocalLobbyChanged(LocalLobby changed)
        {
            if (string.IsNullOrEmpty(changed.LobbyID)) // When the player leaves, their LocalLobby is cleared out but maintained.
                EndTracking();
            m_shouldPushData = true;
        }

        public void OnUpdate(float dt)
        {
            if (m_isAwaitingQuery || m_localLobby == null)
                return;

            m_isAwaitingQuery = true; // Note that because we make async calls, if one of them fails and doesn't call our callback, this will never be reset to false.
            if (m_shouldPushData)
                PushDataToLobby();
            else
                OnRetrieve();

            void PushDataToLobby()
            {
                if (m_localUser == null)
                {
                    m_isAwaitingQuery = false;
                    return; // Don't revert m_shouldPushData yet, so that we can retry.
                }
                m_shouldPushData = false;

                if (m_localUser.IsHost)
                    DoLobbyDataPush();
                else
                    DoPlayerDataPush();
            }

            void DoLobbyDataPush()
            {
                LobbyAsyncRequests.Instance.UpdateLobbyDataAsync(Lobby.ToLocalLobby.RetrieveLobbyData(m_localLobby), () => { DoPlayerDataPush(); });
            }

            void DoPlayerDataPush()
            {
                LobbyAsyncRequests.Instance.UpdatePlayerDataAsync(Lobby.ToLocalLobby.RetrieveUserData(m_localUser), () => { m_isAwaitingQuery = false; });
            }

            void OnRetrieve()
            {
                m_isAwaitingQuery = false;
                LobbyRemote lobby = LobbyAsyncRequests.Instance.CurrentLobby;
                if (lobby == null) return;
                bool prevShouldPush = m_shouldPushData;
                var prevState = m_localLobby.State;
                Lobby.ToLocalLobby.Convert(lobby, m_localLobby, m_localUser);
                m_shouldPushData = prevShouldPush;
                CheckForAllPlayersReady();

                if (prevState != LobbyState.Lobby && m_localLobby.State == LobbyState.Lobby)
                    Locator.Get.Messenger.OnReceiveMessage(MessageType.ToLobby, null);
            }


            void CheckForAllPlayersReady()
            {
                bool areAllPlayersReady = m_localLobby.AllPlayersReadyTime != null;
                if (areAllPlayersReady)
                {
                    long targetTimeTicks = m_localLobby.AllPlayersReadyTime.Value;
                    DateTime targetTime = new DateTime(targetTimeTicks);
                    if (targetTime.Subtract(DateTime.Now).Seconds < 0)
                        return;

                    Locator.Get.Messenger.OnReceiveMessage(MessageType.Client_EndReadyCountdownAt, targetTime); // Note that this could be called multiple times.
                }
            }
        }
    }
}
