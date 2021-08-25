using System;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace LobbyRelaySample.relay
{
    /// <summary>
    /// Wrapper for all the interaction with the Relay API.
    /// </summary>
    public static class RelayAPIInterface
    {
        /// <summary>
        /// A Relay Allocation represents a "server" for a new host.
        /// </summary>
        public static async void AllocateAsync(int maxConnections, Action<Allocation> onComplete)
        {
            try
            {
                Allocation allocation = await Relay.Instance.CreateAllocationAsync(maxConnections);

                onComplete.Invoke(allocation);
            }
            catch (RelayServiceException ex)
            {
                Debug.LogError($"Relay AllocateAsync returned a relay exception: {ex.Reason} - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Only after an Allocation has been completed can a Relay join code be obtained. This code will be stored in the lobby's data as non-public
        /// such that players can retrieve the Relay join code only after connecting to the lobby.
        /// </summary>
        public static async void GetJoinCodeAsync(Guid hostAllocationId, Action<string> onComplete)
        {
            try
            {
                string joinCode = await Relay.Instance.GetJoinCodeAsync(hostAllocationId);
                onComplete.Invoke(joinCode);
            }
            catch (RelayServiceException ex)
                {
                 	Debug.LogError($"Relay GetJoinCodeAsync returned a relay exception: {ex.Reason} - {ex.Message}");
                	throw;
                }
        }

        /// <summary>
        /// Clients call this to retrieve the host's Allocation via a Relay join code.
        /// </summary>
        public static async void JoinAsync(string joinCode, Action<JoinAllocation> onComplete)
        {
            try
            {
                JoinAllocation joinAllocation = await Relay.Instance.JoinAllocationAsync(joinCode);
                onComplete.Invoke(joinAllocation);
            }
            catch (RelayServiceException ex)
            {
              	Debug.LogError($"Relay JoinCodeAsync returned a relay exception: {ex.Reason} - {ex.Message}");
                throw;
            }
        }
    }
}
