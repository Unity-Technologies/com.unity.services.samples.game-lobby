using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace LobbyRelaySample.relay
{
    /// <summary>
    /// Responsible for setting up a connection with Relay using Unity Transport (UTP). A Relay Allocation is created by the host, and then all players
    /// bind UTP to that Allocation in order to send data to each other.
    /// Must be a MonoBehaviour since the binding process doesn't have asynchronous callback options.
    /// </summary>
    public abstract class RelayUtpSetup : MonoBehaviour
    {
        protected bool m_isRelayConnected = false;
        protected NetworkDriver m_networkDriver;
        protected List<NetworkConnection> m_connections;
        protected NetworkEndPoint m_endpointForServer;
        protected LocalLobby m_localLobby;
        protected LobbyUser m_localUser;
        protected Action<bool, RelayUtpClient> m_onJoinComplete;

        public enum MsgType { Ping = 0, NewPlayer, ReadyState, PlayerName, Emote, StartCountdown, CancelCountdown, ConfirmInGame, EndInGame, PlayerDisconnect }

        public void BeginRelayJoin(LocalLobby localLobby, LobbyUser localUser, Action<bool, RelayUtpClient> onJoinComplete)
        {
            m_localLobby = localLobby;
            m_localUser = localUser;
            m_onJoinComplete = onJoinComplete;
            JoinRelay();
        }
        protected abstract void JoinRelay();

        /// <summary>
        /// Shared behavior for binding UTP to the Relay Allocation, which is required for use.
        /// Note that a host will send bytes from the Allocation it creates, whereas a client will send bytes from the JoinAllocation it receives using a relay code.
        /// </summary>
        protected void BindToAllocation(string ip, int port, byte[] allocationIdBytes, byte[] connectionDataBytes, byte[] hostConnectionDataBytes, byte[] hmacKeyBytes, int connectionCapacity)
        {
            NetworkEndPoint     serverEndpoint     = NetworkEndPoint.Parse(ip, (ushort)port);
            RelayAllocationId   allocationId       = ConvertAllocationIdBytes(allocationIdBytes);
            RelayConnectionData connectionData     = ConvertConnectionDataBytes(connectionDataBytes);
            RelayConnectionData hostConnectionData = ConvertConnectionDataBytes(hostConnectionDataBytes);
            RelayHMACKey        key                = ConvertHMACKeyBytes(hmacKeyBytes);

            m_endpointForServer = serverEndpoint;
            //TODO Implement DTLS 
            bool isSecure = false;
            var relayServerData = new RelayServerData(ref serverEndpoint, 0, ref allocationId, ref connectionData, ref hostConnectionData, ref key, isSecure);
            relayServerData.ComputeNewNonce();
            var relayNetworkParameter = new RelayNetworkParameter { ServerData = relayServerData };

            m_networkDriver = NetworkDriver.Create(new INetworkParameter[] { relayNetworkParameter });
            m_connections = new List<NetworkConnection>(connectionCapacity);

            if (m_networkDriver.Bind(NetworkEndPoint.AnyIpv4) != 0)
                Debug.LogError("Failed to bind to Relay allocation.");
            else
                StartCoroutine(WaitForBindComplete());
        }

        private IEnumerator WaitForBindComplete()
        {
            while (!m_networkDriver.Bound)
            {
                m_networkDriver.ScheduleUpdate().Complete();
                yield return null;
            }
            OnBindingComplete();
        }

        protected abstract void OnBindingComplete();

        #region UTP uses pointers instead of managed arrays for performance reasons, so we use these helper functions to convert them.
        unsafe private static RelayAllocationId ConvertAllocationIdBytes(byte[] allocationIdBytes)
        {
            fixed (byte* ptr = allocationIdBytes)
            {
                return RelayAllocationId.FromBytePointer(ptr, allocationIdBytes.Length);
            }
        }

        unsafe private static RelayConnectionData ConvertConnectionDataBytes(byte[] connectionData)
        {
            fixed (byte* ptr = connectionData)
            {
                return RelayConnectionData.FromBytePointer(ptr, RelayConnectionData.k_Length);
            }
        }

        unsafe private static RelayHMACKey ConvertHMACKeyBytes(byte[] hmac)
        {
            fixed (byte* ptr = hmac)
            {
                return RelayHMACKey.FromBytePointer(ptr, RelayHMACKey.k_Length);
            }
        }
        #endregion

        private void OnDestroy()
        {
            if (!m_isRelayConnected && m_networkDriver.IsCreated)
                m_networkDriver.Dispose();
        }
    }

    /// <summary>
    /// Host logic: Request a new Allocation, and then both bind to it and request a join code. Once those are both complete, supply data back to the lobby.
    /// </summary>
    public class RelayUtpSetupHost : RelayUtpSetup
    {
        [Flags]
        private enum JoinState { None = 0, Bound = 1, Joined = 2 }
        private JoinState m_joinState = JoinState.None;
        private Allocation m_allocation;

        protected override void JoinRelay()
        {
            RelayAPIInterface.AllocateAsync(m_localLobby.MaxPlayerCount, OnAllocation);
        }

        private void OnAllocation(Allocation allocation)
        {
            m_allocation = allocation;
            RelayAPIInterface.GetJoinCodeAsync(allocation.AllocationId, OnRelayCode);
            BindToAllocation(allocation.RelayServer.IpV4, allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.ConnectionData, allocation.ConnectionData, allocation.Key, 16);
        }

        private void OnRelayCode(string relayCode)
        {
            m_localLobby.RelayCode = relayCode;
            m_localLobby.RelayServer = new ServerAddress(m_allocation.RelayServer.IpV4, m_allocation.RelayServer.Port);
            m_joinState |= JoinState.Joined;
            CheckForComplete();
        }

        protected override void OnBindingComplete()
        {
            if (m_networkDriver.Listen() != 0)
            {
                Debug.LogError("RelayUtpSetupHost failed to bind to the Relay Allocation.");
                m_onJoinComplete(false, null);
            }
            else
            {
                Debug.Log("Relay host is bound.");
                m_joinState |= JoinState.Bound;
                CheckForComplete();
            }
        }

        private void CheckForComplete()
        {
            if (m_joinState == (JoinState.Joined | JoinState.Bound) && this != null) // this will equal null (i.e. this component has been destroyed) if the host left the lobby during the Relay connection sequence.
            {
                m_isRelayConnected = true;
                RelayUtpHost host = gameObject.AddComponent<RelayUtpHost>();
                host.Initialize(m_networkDriver, m_connections, m_localUser, m_localLobby);
                m_onJoinComplete(true, host);
                LobbyAsyncRequests.Instance.UpdatePlayerRelayInfoAsync(m_allocation.AllocationId.ToString(), m_localLobby.RelayCode, null);
            }
        }
    }

    /// <summary>
    /// Client logic: Wait until the Relay join code is retrieved from the lobby's shared data. Then, use that code to get the Allocation to bind to, and
    /// then create a connection to the host.
    /// </summary>
    public class RelayUtpSetupClient : RelayUtpSetup
    {
        private JoinAllocation m_allocation;

        protected override void JoinRelay()
        {
            m_localLobby.onChanged += OnLobbyChange;
        }

        private void OnLobbyChange(LocalLobby lobby)
        {
            if (m_localLobby.RelayCode != null)
            {
                RelayAPIInterface.JoinAsync(m_localLobby.RelayCode, OnJoin);
                m_localLobby.onChanged -= OnLobbyChange;
            }
        }

        private void OnJoin(JoinAllocation joinAllocation)
        {
            if (joinAllocation == null || this == null) // The returned JoinAllocation is null if allocation failed. this would be destroyed already if you quit the lobby while Relay is connecting.
                return;
            m_allocation = joinAllocation;
            BindToAllocation(joinAllocation.RelayServer.IpV4, joinAllocation.RelayServer.Port, joinAllocation.AllocationIdBytes, joinAllocation.ConnectionData, joinAllocation.HostConnectionData, joinAllocation.Key, 1);
            m_localLobby.RelayServer = new ServerAddress(joinAllocation.RelayServer.IpV4, joinAllocation.RelayServer.Port);
        }

        protected override void OnBindingComplete()
        {
            StartCoroutine(ConnectToServer());
        }

        private IEnumerator ConnectToServer()
        {
            // Once the client is bound to the Relay server, send a connection request.
            m_connections.Add(m_networkDriver.Connect(m_endpointForServer));
            while (m_networkDriver.GetConnectionState(m_connections[0]) == NetworkConnection.State.Connecting)
            {
                m_networkDriver.ScheduleUpdate().Complete();
                yield return null;
            }
            if (m_networkDriver.GetConnectionState(m_connections[0]) != NetworkConnection.State.Connected)
            {
                Debug.LogError("RelayUtpSetupClient could not connect to the host.");
                m_onJoinComplete(false, null);
            }
            else if (this != null)
            {
                m_isRelayConnected = true;
                RelayUtpClient client = gameObject.AddComponent<RelayUtpClient>();
                client.Initialize(m_networkDriver, m_connections, m_localUser, m_localLobby);
                m_onJoinComplete(true, client);
                LobbyAsyncRequests.Instance.UpdatePlayerRelayInfoAsync(m_allocation.AllocationId.ToString(), m_localLobby.RelayCode, null);
            }
        }
    }
}
