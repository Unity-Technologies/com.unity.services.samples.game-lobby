using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace LobbyAsync
{
	public class LobbyRelayTest
	{


		public async Task Join(string lobbyId, string password = "")
		{
			try
			{/*
				// Authenticate
				await AuthenticationService.Instance.SignInAnonymouslyAsync();

				// Join Lobby
				LobbyPlayer player = new LobbyPlayer(AuthenticationService.Instance.PlayerId);
				JoinLobbyByIdOptions options = new JoinLobbyByIdOptions()
				{
					Player = player
				};
				Lobby lobby = = await LobbyHelper.Instance.JoinLobbyByIdAsync(lobbyId, options);

				// Join Relay
				string joinCode = lobby.Data["joinCode"].Value;
				JoinAllocation join = await Relay.Instance.JoinAllocationAsync(joinCode);
				await Lobbies.Instance.UpdatePlayerAsync(lobby.Id, player.Id, new UpdatePlayerOptions()
				{
					AllocationId = join.AllocationId.ToString(),
					ConnectionInfo = joinCode
				});
				relayTransport.SetClientRelayData(join.RelayServer.IpV4, (ushort) join.RelayServer.Port,
					join.AllocationIdBytes, join.Key, join.ConnectionData, join.HostConnectionData, true);

				// Start Client
				NetworkManager.Singleton.StartClient();*/
			}
			catch (Exception e)
			{
				Debug.LogError(e);
			}
		}



		public async Task Create()
		{

			try
			{
				/*
				// Authenticate
				await Authenticate();


				// Allocate Relay
				Allocation allocation = await Relay.Instance.CreateAllocationAsync(maxPlayers);
				relayTransport.SetHostRelayData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, true);

				// Generate Join Code
				string joinCode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);

				// Create Lobby
				CreateLobbyOptions options = new CreateLobbyOptions()
				{
					IsPrivate = isPrivate,
					Data = new Dictionary<string, DataObject>()
					{
						{ "joinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) },
					},
					Player = new LobbyPlayer(AuthenticationService.Instance.PlayerId, joinCode, null, allocation.AllocationId.ToString())
				};
				Lobby lobby = await LobbyHelper.Instance.CreateLobbyAsync(worldNameInputField.text, maxPlayers, options);

				// Start Host
				NetworkManager.Singleton.StartHost();*/
			}
			catch (Exception e)
			{
				Debug.LogError(e);
			}
		}

	}

}
