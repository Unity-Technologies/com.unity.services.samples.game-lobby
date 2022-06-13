using System;
using System.Threading.Tasks;
using LobbyRelaySample.lobby;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace LobbyRelaySample
{
    /// <summary>
    /// Keep updated on changes to a joined lobby, at a speed compliant with Lobby's rate limiting.
    /// </summary>
    public class LobbySynchronizer : IReceiveMessages, IDisposable
    {
        LocalLobby m_LocalLobby;
        LobbyUser m_LocalUser;
        LobbyManager m_LobbyManager;
        bool m_LocalChanges = false;

        const int
            k_approvalMaxMS = 10000; // Used for determining if a user should timeout if they are unable to connect.

        int m_lifetime = 0;
        const int k_UpdateIntervalMS = 1000;

        public LobbySynchronizer(LobbyManager lobbyManager)
        {
            m_LobbyManager = lobbyManager;
        }

        public void StartSynch(LocalLobby localLobby, LobbyUser localUser)
        {
            m_LocalUser = localUser;
            m_LocalLobby = localLobby;
            m_LocalLobby.onChanged += OnLocalLobbyChanged;
            m_LocalChanges = true;
            Locator.Get.Messenger.Subscribe(this);
#pragma warning disable 4014
            UpdateLoopAsync();
#pragma warning restore 4014
            m_lifetime = 0;
        }

        public void EndSynch()
        {
            m_LocalChanges = false;

            Locator.Get.Messenger.Unsubscribe(this);
            if (m_LocalLobby != null)
                m_LocalLobby.onChanged -= OnLocalLobbyChanged;

            m_LocalLobby = null;
        }

        //TODO Stop players from joining lobby while game is underway.
        public void OnReceiveMessage(MessageType type, object msg)
        {
//            if (type == MessageType.ClientUserSeekingDisapproval)
//            {
//                bool shouldDisapprove =
//                    m_LocalLobby.State !=
//                    LobbyState.Lobby; // By not refreshing, it's possible to have a lobby in the lobby list UI after its countdown starts and then try joining.
//                if (shouldDisapprove)
//                    (msg as Action<relay.Approval>)?.Invoke(relay.Approval.GameAlreadyStarted);
//            }
        }

        /// <summary>
        /// If there have been any data changes since the last update, push them to Lobby. Regardless, pull for the most recent data.
        /// (Unless we're already awaiting a query, in which case continue waiting.)
        /// </summary>
        async Task UpdateLoopAsync()
        {
            Lobby latestLobby = null;

            while (m_LocalLobby != null)
            {
                if (m_LocalChanges)
                {
                    m_LocalLobby.changedByLobbySynch = true;
                    latestLobby = await PushDataToLobby();
                }
                else
                    latestLobby = await m_LobbyManager.GetLobbyAsync();

                if (IfRemoteLobbyChanged(latestLobby))
                    LobbyConverters.RemoteToLocal(latestLobby, m_LocalLobby);
                m_LocalLobby.changedByLobbySynch = false;

                if (!LobbyHasHost())
                {
                    LeaveLobbyBecauseNoHost();
                    break;
                }

                m_lifetime += k_UpdateIntervalMS;
                await Task.Delay(k_UpdateIntervalMS);
            }

            bool IfRemoteLobbyChanged(Lobby remoteLobby)
            {
                var remoteLobbyTime = remoteLobby.LastUpdated.ToFileTimeUtc();
                var localLobbyTime = m_LocalLobby.Data.LastEdit;
                var isLocalOutOfDate = remoteLobbyTime > localLobbyTime;
                return isLocalOutOfDate;
            }

            async Task<Lobby> PushDataToLobby()
            {
                m_LocalChanges = false;

                if (m_LocalUser.IsHost)
                    await m_LobbyManager.UpdateLobbyDataAsync(
                        LobbyConverters.LocalToRemoteData(m_LocalLobby));

                return await m_LobbyManager.UpdatePlayerDataAsync(
                    LobbyConverters.LocalToRemoteUserData(m_LocalUser));
            }

            bool LobbyHasHost()
            {
                if (!m_LocalUser.IsHost)
                {
                    foreach (var lobbyUser in m_LocalLobby.LobbyUsers)
                    {
                        if (lobbyUser.Value.IsHost)
                            return true;
                    }

                    return false;
                }

                return true;
            }

            void LeaveLobbyBecauseNoHost()
            {
                Locator.Get.Messenger.OnReceiveMessage(MessageType.DisplayErrorPopup,
                    "Host left the lobby! Disconnecting...");
                Locator.Get.Messenger.OnReceiveMessage(MessageType.EndGame, null);
                Locator.Get.Messenger.OnReceiveMessage(MessageType.ChangeMenuState, GameState.JoinMenu);
            }
        }

        void OnLocalLobbyChanged(LocalLobby localLobby)
        {
            if (string.IsNullOrEmpty(localLobby.LobbyID)
            ) // When the player leaves, their LocalLobby is cleared out.
            {
                EndSynch();
                return;
            }

            //Catch for infinite update looping from the synchronizer.
            if (localLobby.changedByLobbySynch)
            {
                return;
            }

            m_LocalChanges = true;
        }

        public void Dispose()
        {
            EndSynch();
        }
    }
}