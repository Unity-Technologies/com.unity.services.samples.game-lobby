using System;
using System.Threading.Tasks;
using LobbyRelaySample.lobby;
using Unity.Services.Lobbies.Models;

namespace LobbyRelaySample
{
	/// <summary>
	/// Keep updated on changes to a joined lobby, at a speed compliant with Lobby's rate limiting.
	/// </summary>
	public class LobbyUpdater : IReceiveMessages, IDisposable
	{
		LocalLobby m_LocalLobby;
		LobbyUser m_LocalUser;
		LobbyManager m_LobbyManager;
		bool m_ShouldPushData = false;

		const int
			k_approvalMaxMS = 10000; // Used for determining if a user should timeout if they are unable to connect.

		int m_lifetime = 0;
		const int k_UpdateIntervalMS = 100;

		public LobbyUpdater(LobbyManager lobbyManager)
		{
			m_LobbyManager = lobbyManager;
		}

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
				bool shouldDisapprove =
					m_LocalLobby.State !=
					LobbyState.Lobby; // By not refreshing, it's possible to have a lobby in the lobby list UI after its countdown starts and then try joining.
				if (shouldDisapprove)
					(msg as Action<relay.Approval>)?.Invoke(relay.Approval.GameAlreadyStarted);
			}
		}


		/// <summary>
		/// If there have been any data changes since the last update, push them to Lobby. Regardless, pull for the most recent data.
		/// (Unless we're already awaiting a query, in which case continue waiting.)
		/// </summary>
		async Task UpdateLoopAsync()
		{
			while (m_LocalLobby != null)
			{
				if (!m_LocalUser.IsApproved && m_lifetime > k_approvalMaxMS)
				{
					Locator.Get.Messenger.OnReceiveMessage(MessageType.DisplayErrorPopup,
						"Connection attempt timed out!");
					Locator.Get.Messenger.OnReceiveMessage(MessageType.ChangeMenuState, GameState.JoinMenu);
				}

				if (m_ShouldPushData)
					await PushDataToLobby();
				else
					UpdateLocalLobby();


				m_lifetime += k_UpdateIntervalMS;
				await Task.Delay(k_UpdateIntervalMS);
			}

			async Task PushDataToLobby()
			{
				m_ShouldPushData = false;

				if (m_LocalUser.IsHost)
					m_LobbyManager.UpdateLobbyDataAsync(m_LobbyManager.CurrentLobby,
						LobbyConverters.LocalToRemoteData(m_LocalLobby));
				m_LobbyManager.UpdatePlayerDataAsync(m_LobbyManager.CurrentLobby.Id,
					LobbyConverters.LocalToRemoteUserData(m_LocalUser));
			}


			void UpdateLocalLobby()
			{
				m_LocalLobby.canPullUpdate = true;

				//synching our local lobby
				LobbyConverters.RemoteToLocal(m_LobbyManager.CurrentLobby, m_LocalLobby);

				if (!m_LocalUser.IsHost)
				{
					foreach (var lobbyUser in m_LocalLobby.LobbyUsers)
					{
						if (lobbyUser.Value.IsHost)
							return;
					}

					Locator.Get.Messenger.OnReceiveMessage(MessageType.DisplayErrorPopup,
						"Host left the lobby! Disconnecting...");
					Locator.Get.Messenger.OnReceiveMessage(MessageType.EndGame, null);
					Locator.Get.Messenger.OnReceiveMessage(MessageType.ChangeMenuState, GameState.JoinMenu);
				}
			}
		}

		void OnLocalLobbyChanged(LocalLobby localLobby)
		{
			if (string.IsNullOrEmpty(localLobby.LobbyID)
			) // When the player leaves, their LocalLobby is cleared out.
			{
				EndTracking();
				return;
			}

			if (localLobby.canPullUpdate)
			{
				localLobby.canPullUpdate = false;
				return;
			}

			m_ShouldPushData = true;
		}


		public void Dispose()
		{
			EndTracking();
		}
	}
}
