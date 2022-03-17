using System;
using Unity.Services.Relay.Models;
using UnityEngine;
using RelayService = Unity.Services.Relay.Relay;

namespace LobbyRelaySample.relay
{
    /// <summary>
    /// Wrapper for all the interaction with the Relay API.
    /// Relay acts as an intermediary between hosts and clients for privacy. Each player will connect to an obfuscated IP address provided by Relay as though connecting directly to other players.
    /// </summary>
    public static class RelayAPIInterface
    {
        /// <summary>
        /// A Relay Allocation represents a "server" for a new host.
        /// </summary>
        public static void AllocateAsync(int maxConnections, Action<Allocation> onComplete)
        {
            var task = RelayService.Instance.CreateAllocationAsync(maxConnections);
            AsyncRequestRelay.Instance.DoRequest(task, OnResponse);

            void OnResponse(Allocation response)
            {
                if (response == null)
                    Debug.LogError("Relay returned a null Allocation. This might occur if the Relay service has an outage, if your cloud project ID isn't linked, or if your Relay package version is outdated.");
                else
                    onComplete?.Invoke(response);
            };
        }

        /// <summary>
        /// Only after an Allocation has been completed can a Relay join code be obtained. This code will be stored in the lobby's data as non-public
        /// such that players can retrieve the Relay join code only after connecting to the lobby. (Note that this is not the same as the lobby code.)
        /// </summary>
        public static void GetJoinCodeAsync(Guid hostAllocationId, Action<string> onComplete)
        {
            var task = RelayService.Instance.GetJoinCodeAsync(hostAllocationId);
            AsyncRequestRelay.Instance.DoRequest(task, OnResponse);

            void OnResponse(string response)
            {
                if (response == null)
                    Debug.LogError("Could not retrieve a Relay join code.");
                else
                    onComplete?.Invoke(response);
            }
        }

        /// <summary>
        /// Clients call this to retrieve the host's Allocation via a Relay join code.
        /// </summary>
        public static void JoinAsync(string joinCode, Action<JoinAllocation> onComplete)
        {
            var task = RelayService.Instance.JoinAllocationAsync(joinCode);
            AsyncRequestRelay.Instance.DoRequest(task, OnResponse);

            void OnResponse(JoinAllocation response)
            {
                if (response == null)
                    Debug.LogError("Could not join async with Relay join code " + joinCode);
                else
                    onComplete?.Invoke(response);
            };
        }
    }
}
