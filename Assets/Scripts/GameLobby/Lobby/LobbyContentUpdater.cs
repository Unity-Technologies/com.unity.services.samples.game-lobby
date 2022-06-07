using System;
using System.Threading.Tasks;
using LobbyRelaySample.lobby;
using Unity.Services.Lobbies.Models;

namespace LobbyRelaySample
{
    /// <summary>
    /// Keep updated on changes to a joined lobby, at a speed compliant with Lobby's rate limiting.
    /// </summary>
    public class LobbyContentUpdater : IReceiveMessages
    {
        private LocalLobby m_LocalLobby;
        private LobbyUser m_LocalUser;
        private bool m_ShouldPushData = false;

        private const float k_approvalMaxTime = 10; // Used for determining if a user should timeout if they are unable to connect.
        private float m_lifetime = 0;
        const int k_UpdateIntervalMS = 1500;

        public void BeginTracking(LocalLobby localLobby, LobbyUser localUser)
        {
            m_LocalUser = localUser;
            m_LocalLobby = localLobby;
            m_LocalLobby.onChanged += OnLocalLobbyChanged;
            m_ShouldPushData = true;
            Locator.Get.Messenger.Subscribe(this);
#pragma warning disable 4014
            UpdateLoopAsync();

#pragma warning restore 4014
            m_lifetime = 0;
        }

        public void EndTracking()
        {
            m_ShouldPushData = false;

            Locator.Get.Messenger.Unsubscribe(this);
            if (m_LocalLobby != null)
                m_LocalLobby.onChanged -= OnLocalLobbyChanged;

            m_LocalLobby = null;
        }

        public void OnReceiveMessage(MessageType type, object msg)
        {
            if (type == MessageType.ClientUserSeekingDisapproval)
            {
                bool shouldDisapprove = m_LocalLobby.State != LobbyState.Lobby; // By not refreshing, it's possible to have a lobby in the lobby list UI after its countdown starts and then try joining.
                if (shouldDisapprove)
                    (msg as Action<relay.Approval>)?.Invoke(relay.Approval.GameAlreadyStarted);
            }
        }

        void OnLocalLobbyChanged(LocalLobby changed)
        {
            if (string.IsNullOrEmpty(changed.LobbyID)) // When the player leaves, their LocalLobby is cleared out but maintained.
            {
                EndTracking();
                return;
            }

            if (changed.canPullUpdate)
            {
                changed.canPullUpdate = false;
                return;
            }

            m_ShouldPushData = true;
        }

        /// <summary>
        /// If there have been any data changes since the last update, push them to Lobby. Regardless, pull for the most recent data.
        /// (Unless we're already awaiting a query, in which case continue waiting.)
        /// </summary>
        private async Task UpdateLoopAsync()
        {
            while (m_LocalLobby != null)
            {
                if (!m_LocalUser.IsApproved && m_lifetime > k_approvalMaxTime)
                {
                    Locator.Get.Messenger.OnReceiveMessage(MessageType.DisplayErrorPopup, "Connection attempt timed out!");
                    Locator.Get.Messenger.OnReceiveMessage(MessageType.ChangeMenuState, GameState.JoinMenu);
                }

                if (m_ShouldPushData)
                    PushDataToLobby();
                else
                    UpdateLocalLobby();

                void PushDataToLobby()
                {
                    m_ShouldPushData = false;

                    if (m_LocalUser.IsHost)
                    {
                        DoLobbyDataPush();
                    }

                    DoPlayerDataPush();
                }

                void DoLobbyDataPush()
                {
#pragma warning disable 4014
                    LobbyAsyncRequests.Instance.UpdateLobbyDataAsync(LobbyConverters.LocalToRemoteData(m_LocalLobby));
#pragma warning restore 4014
                }

                void DoPlayerDataPush()
                {
#pragma warning disable 4014
                    LobbyAsyncRequests.Instance.UpdatePlayerDataAsync(LobbyConverters.LocalToRemoteUserData(m_LocalUser));
#pragma warning restore 4014
                }

                await Task.Delay(k_UpdateIntervalMS);
            }
        }

        void UpdateLocalLobby()
        {
            var remoteLobby = LobbyAsyncRequests.Instance.CurrentLobby;
            if (remoteLobby == null)
                return;
            m_LocalLobby.canPullUpdate = true;

            //synching our local lobby
            LobbyConverters.RemoteToLocal(remoteLobby, m_LocalLobby);

            //Dont push data this tick, since we "pulled"s
            if (!m_LocalUser.IsHost)
            {
                foreach (var lobbyUser in m_LocalLobby.LobbyUsers)
                {
                    if (lobbyUser.Value.IsHost)
                        return;
                }

                Locator.Get.Messenger.OnReceiveMessage(MessageType.DisplayErrorPopup, "Host left the lobby! Disconnecting...");
                Locator.Get.Messenger.OnReceiveMessage(MessageType.EndGame, null);
                Locator.Get.Messenger.OnReceiveMessage(MessageType.ChangeMenuState, GameState.JoinMenu);
            }
        }
    }
}
