using System;
using System.Collections.Generic;
using LobbyRemote = Unity.Services.Lobbies.Models.Lobby;

namespace LobbyRelaySample
{

    // TODO: It might make sense to change UpdateSlow to, rather than have a fixed cycle on which everything is bound, be able to track when each thing should update?
    // I.e. what I want here now is for when a lobby async request comes in, if it has already been long enough, it immediately fires and then sets a cooldown.

    // This is still necessary for detecting new players, although I think we could hit a case where the relay join ends up coming in before the cooldown?
    // So, we should be able to create a new LobbyUser that way as well.
    // That is, creating a (local) player via Relay or via Lobby should go through the same mechanism...? Or do we hold onto the Relay data until the player exists?

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
                LobbyAsyncRequests.Instance.UpdateLobbyDataAsync(RetrieveLobbyData(m_localLobby), () => { DoPlayerDataPush(); });
            }

            void DoPlayerDataPush()
            {
                LobbyAsyncRequests.Instance.UpdatePlayerDataAsync(RetrieveUserData(m_localUser), () => { m_isAwaitingQuery = false; });
            }

            void OnRetrieve()
            {
                m_isAwaitingQuery = false;
                LobbyRemote lobbyRemote = LobbyAsyncRequests.Instance.CurrentLobby;
                if (lobbyRemote == null) return;
                bool prevShouldPush = m_shouldPushData;
                var prevState = m_localLobby.State;
                lobby.ToLocalLobby.Convert(lobbyRemote, m_localLobby);
                m_shouldPushData = prevShouldPush;
            }
        }

        public static Dictionary<string, string> RetrieveLobbyData(LocalLobby lobby)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("RelayCode", lobby.RelayCode);
            data.Add("State", ((int)lobby.State).ToString());
            // We only want the ArePlayersReadyTime to be set when we actually are ready for it, and it's null otherwise. So, don't set that here.
            return data;
        }

        public static Dictionary<string, string> RetrieveUserData(LobbyUser user)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            if (user == null || string.IsNullOrEmpty(user.ID))
                return data;
            data.Add("DisplayName", user.DisplayName); // The lobby doesn't need to know any data beyond the name and state; Relay will handle the rest.
            data.Add("UserStatus", ((int)user.UserStatus).ToString());
            return data;
        }
    }
}
