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
        /// API calls are asynchronous, but for debugging and other reasons we want to reify them as objects so that they can be monitored.
        /// </summary>
        private class InProgressRequest<T>
        {
            public InProgressRequest(Task<T> task, Action<T> onComplete)
            {
                DoRequest(task, onComplete);
            }

            private async void DoRequest(Task<T> task, Action<T> onComplete)
            {
                T result = default;
                string currentTrace = System.Environment.StackTrace; // If we don't get the calling context here, it's lost once the async operation begins.
                try {
                    result = await task;
                } catch (Exception e) {
                    Exception eFull = new Exception($"Call stack before async call:\n{currentTrace}\n", e);
                    throw eFull;
                } finally {
                    onComplete?.Invoke(result);
                }
            }
        }

        /// <summary>
        /// A Relay Allocation represents a "server" for a new host.
        /// </summary>
        public static void AllocateAsync(int maxConnections, Action<Allocation> onComplete)
        {
            CreateAllocationRequest createAllocationRequest = new CreateAllocationRequest(new AllocationRequest(maxConnections));
            var task = RelayService.AllocationsApiClient.CreateAllocationAsync(createAllocationRequest);
            new InProgressRequest<Response<AllocateResponseBody>>(task, OnResponse);

            void OnResponse(Response<AllocateResponseBody> response)
            {
                if (response == null)
                    Debug.LogError("Relay returned a null Allocation. It's possible the Relay service is currently down.");
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
                    Debug.LogError($"Join Code Get returned a non Success code: {a.Status}");
                }
            });
        }
        private static void GetJoinCodeAsync(Guid hostAllocationId, Action<Response<JoinCodeResponseBody>> onComplete)
        {
            CreateJoincodeRequest joinCodeRequest = new CreateJoincodeRequest(new JoinCodeRequest(hostAllocationId));
            var task = RelayService.AllocationsApiClient.CreateJoincodeAsync(joinCodeRequest);
            new InProgressRequest<Response<JoinCodeResponseBody>>(task, onComplete);
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
            new InProgressRequest<Response<JoinResponseBody>>(task, onComplete);
        }
    }
}
