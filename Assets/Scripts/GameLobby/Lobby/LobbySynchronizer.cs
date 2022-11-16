using System;
using System.Threading.Tasks;
using LobbyRelaySample.lobby;
using Unity.Services.Lobbies.Models;
using UnityEngine;

// namespace LobbyRelaySample
// {
//     /// <summary>
//     /// Keep updated on changes to a joined lobby, at a speed compliant with Lobby's rate limiting.
//     /// </summary>
//     public class LobbySynchronizer : IDisposable
//     {
//         LocalLobby m_LocalLobby;
//         LocalPlayer m_LocalUser;
//         LobbyManager m_LobbyManager;
//         bool m_LocalChanges = false;
//
//         const int k_approvalMaxMS = 10000; // Used for determining if a user should timeout if they are unable to connect.
//
//         int m_lifetime = 0;
//         const int k_UpdateIntervalMS = 1000;
//
//         public LobbySynchronizer(LobbyManager lobbyManager)
//         {
//             m_LobbyManager = lobbyManager;
//         }
//
//         public void StartSynch(LocalLobby localLobby, LocalPlayer localUser)
//         {
//             m_LocalUser = localUser;
//             m_LocalLobby = localLobby;
//             m_LocalLobby.LobbyID.onChanged += OnLobbyIdChanged;
//             m_LocalChanges = true;
//             #pragma warning disable 4014
//             UpdateLoopAsync();
//             #pragma warning restore 4014
//             m_lifetime = 0;
//         }
//
//
//         public void EndSynch()
//         {
//             m_LocalChanges = false;
//
//             if (m_LocalLobby != null)
//                 m_LocalLobby.LobbyID.onChanged -= OnLobbyIdChanged;
//
//             m_LocalLobby = null;
//         }
//
//         /// <summary>
//         /// If there have been any data changes since the last update, push them to Lobby. Regardless, pull for the most recent data.
//         /// (Unless we're already awaiting a query, in which case continue waiting.)
//         /// </summary>
//         async Task UpdateLoopAsync()
//         {
//             Lobby latestLobby = null;
//
//             while (m_LocalLobby != null)
//             {
//                 latestLobby = await GetLatestRemoteLobby();
//
//                 if (IfRemoteLobbyChanged(latestLobby))
//                 {
//                     //Pulling remote changes, and applying them to the local lobby usually flags it as changed,
//                     //Causing another pull, the RemoteToLocal converter ensures this does not happen by flagging the lobby.
//                     LobbyConverters.RemoteToLocal(latestLobby, m_LocalLobby, false);
//                 }
//                 Debug.Log(m_LocalLobby.ToString());
//
//                 if (!LobbyHasHost())
//                 {
//                     LeaveLobbyBecauseNoHost();
//                     break;
//                 }
//
//                 var areAllusersReady = AreAllUsersReady();
//                 if (areAllusersReady && m_LocalLobby.LocalLobbyState.Value == LobbyState.Lobby)
//                 {
//                     GameManager.Instance.BeginCountdown();
//                 }
//                 else if (!areAllusersReady && m_LocalLobby.LocalLobbyState.Value == LobbyState.CountDown)
//                 {
//                     GameManager.Instance.CancelCountDown();
//                 }
//
//                 m_lifetime += k_UpdateIntervalMS;
//                 await Task.Delay(k_UpdateIntervalMS);
//             }
//         }
//
//         async Task<Lobby> GetLatestRemoteLobby()
//         {
//             Lobby latestLobby = null;
//             if (m_LocalLobby.IsLobbyChanged())
//             {
//                 latestLobby = await PushDataToLobby();
//             }
//             else
//             {
//                 latestLobby = await m_LobbyManager.GetLobbyAsync();
//             }
//
//             return latestLobby;
//         }
//
//         bool IfRemoteLobbyChanged(Lobby remoteLobby)
//         {
//             var remoteLobbyTime = remoteLobby.LastUpdated.ToFileTimeUtc();
//             var localLobbyTime = m_LocalLobby.LastUpdated.Value;
//             var isLocalOutOfDate = remoteLobbyTime > localLobbyTime;
//             return isLocalOutOfDate;
//         }
//
//         async Task<Lobby> PushDataToLobby()
//         {
//             m_LocalChanges = false;
//
//             if (m_LocalUser.IsHost.Value)
//                 await m_LobbyManager.UpdateLobbyDataAsync(
//                     LobbyConverters.LocalToRemoteData(m_LocalLobby));
//
//             return await m_LobbyManager.UpdatePlayerDataAsync(
//                 LobbyConverters.LocalToRemoteUserData(m_LocalUser));
//         }
//
//         bool AreAllUsersReady()
//         {
//             foreach (var lobbyUser in m_LocalLobby.LocalPlayers.Values)
//             {
//                 if (lobbyUser.UserStatus.Value != UserStatus.Ready)
//                 {
//                     return false;
//                 }
//             }
//
//             return true;
//         }
//
//         bool LobbyHasHost()
//         {
//             if (!m_LocalUser.IsHost.Value)
//             {
//                 foreach (var lobbyUser in m_LocalLobby.LocalPlayers)
//                 {
//                     if (lobbyUser.Value.IsHost.Value)
//                         return true;
//                 }
//
//                 return false;
//             }
//
//             return true;
//         }
//
//         void LeaveLobbyBecauseNoHost()
//         {
//             LogHandlerSettings.Instance.SpawnErrorPopup(
//                 "Host left the lobby! Disconnecting...");
//             Locator.Get.Messenger.OnReceiveMessage(MessageType.EndGame, null);
//             GameManager.Instance.ChangeMenuState(GameState.JoinMenu);
//         }
//
//         public void OnLobbyIdChanged(string lobbyID)
//         {
//             if (string.IsNullOrEmpty(lobbyID)
//             ) // When the player leaves, their LocalLobby is cleared out.
//             {
//                 EndSynch();
//             }
//         }
//
//         public void Dispose()
//         {
//             EndSynch();
//         }
//     }
// }
