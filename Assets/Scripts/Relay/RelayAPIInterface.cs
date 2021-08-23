using System;
using System.Threading.Tasks;
using Unity.Services.Relay;
using Unity.Services.Relay.Allocations;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace LobbyRelaySample.Relay
{
    /// <summary>
    /// Wrapper for all the interaction with the Relay API.
    /// </summary>
    public static class RelayAPIInterface
    {
        /// <summary>
        /// A Relay Allocation represents a "server" for a new host.
        /// </summary>
        public static void AllocateAsync(int maxConnections, Action<Allocation> onComplete)
        {
            CreateAllocationRequest createAllocationRequest = new CreateAllocationRequest(new AllocationRequest(maxConnections));
            var task = RelayService.AllocationsApiClient.CreateAllocationAsync(createAllocationRequest);
            AsyncRequest.DoRequest(task, OnResponse);

            void OnResponse(Response<AllocateResponseBody> response)
            {
                if (response == null)
                    Debug.LogError("Relay returned a null Allocation. This might occur if the Relay service has an outage, if your cloud project ID isn't linked, or if your Relay package version is outdated.");
                else if (response.Status >= 200 && response.Status < 300)
                    onComplete?.Invoke(response.Result.Data.Allocation);
                else
                    Debug.LogError($"Allocation returned a non Success code: {response.Status}");
            };
        }

        /// <summary>
        /// Only after an Allocation has been completed can a Relay join code be obtained. This code will be stored in the lobby's data as non-public
        /// such that players can retrieve the Relay join code only after connecting to the lobby.
        /// </summary>
        public static void GetJoinCodeAsync(Guid hostAllocationId, Action<string> onComplete)
        {
            GetJoinCodeAsync(hostAllocationId, a =>
            {
                if (a.Status >= 200 && a.Status < 300)
                    onComplete.Invoke(a.Result.Data.JoinCode);
                else
                {
                    Debug.LogError($"Relay GetJoinCodeAsync returned a non-success code: {a.Status}");
                }
            });
        }
        private static void GetJoinCodeAsync(Guid hostAllocationId, Action<Response<JoinCodeResponseBody>> onComplete)
        {
            CreateJoincodeRequest joinCodeRequest = new CreateJoincodeRequest(new JoinCodeRequest(hostAllocationId));
            var task = RelayService.AllocationsApiClient.CreateJoincodeAsync(joinCodeRequest);
            AsyncRequest.DoRequest(task, onComplete);
        }

        /// <summary>
        /// Clients call this to retrieve the host's Allocation via a Relay join code.
        /// </summary>
        public static void JoinAsync(string joinCode, Action<JoinAllocation> onComplete)
        {
            JoinAsync(joinCode, a =>
            {
                if (a.Status >= 200 && a.Status < 300)
                    onComplete.Invoke(a.Result.Data.Allocation);
                else
                {
                    Debug.LogError($"Join Call returned a non Success code: {a.Status}");
                }
            });
        }

        public static void JoinAsync(string joinCode, Action<Response<JoinResponseBody>> onComplete)
        {
            JoinRelayRequest joinRequest = new JoinRelayRequest(new JoinRequest(joinCode));
            var task = RelayService.AllocationsApiClient.JoinRelayAsync(joinRequest);
            AsyncRequest.DoRequest(task, onComplete);
        }
    }
}
