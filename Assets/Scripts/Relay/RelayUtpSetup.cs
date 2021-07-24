using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace LobbyRelaySample.Relay
{
    /// <summary>
    /// Responsible for setting up a connection with Relay using UTP, for the lobby host.
    /// Must be a MonoBehaviour since the binding process doesn't have asynchronous callback options.
    /// </summary>
    public abstract class RelayUtpSetup : MonoBehaviour
    {
        protected bool m_isRelayConnected = false;
        protected NetworkDriver m_networkDriver;
        protected List<NetworkConnection> m_connections;
        protected NetworkEndPoint m_endpointForServer;
        protected JobHandle m_currentUpdateHandle;
        protected LocalLobby m_localLobby;
        protected LobbyUser m_localUser;
        protected Action<bool, RelayUtpClient> m_onJoinComplete;

        public enum MsgType { NewPlayer = 0, Ping = 1, ReadyState = 2, PlayerName = 3, Emote = 4 }

        public void BeginRelayJoin(LocalLobby localLobby, LobbyUser localUser, Action<bool, RelayUtpClient> onJoinComplete)
        {
            m_localLobby = localLobby;
            m_localUser = localUser;
            m_onJoinComplete = onJoinComplete;
            JoinRelay();
        }
        protected abstract void JoinRelay();

        protected void BindToAllocation(string ip, int port, byte[] allocationIdBytes, byte[] connectionDataBytes, byte[] hostConnectionDataBytes, byte[] hmacKeyBytes, int connectionCapacity)
        {
            NetworkEndPoint     serverEndpoint     = NetworkEndPoint.Parse(ip, (ushort)port);
            RelayAllocationId   allocationId       = ConvertAllocationIdBytes(allocationIdBytes);
            RelayConnectionData connectionData     = ConvertConnectionDataBytes(connectionDataBytes);
            RelayConnectionData hostConnectionData = ConvertConnectionDataBytes(hostConnectionDataBytes);
            RelayHMACKey        key                = ConvertHMACKeyBytes(hmacKeyBytes);

            m_endpointForServer = serverEndpoint;
            var relayServerData = new RelayServerData(ref serverEndpoint, 0, ref allocationId, ref connectionData, ref hostConnectionData, ref key);
            relayServerData.ComputeNewNonce();
            var relayNetworkParameter = new RelayNetworkParameter { ServerData = relayServerData };

            m_networkDriver = NetworkDriver.Create(new INetworkParameter[] { relayNetworkParameter });
            m_connections = new List<NetworkConnection>(connectionCapacity);

            if (m_networkDriver.Bind(NetworkEndPoint.AnyIpv4) != 0)
                Debug.LogError("Failed to bind to Relay allocation.");
            else
                StartCoroutine(WaitForBindComplete()); // TODO: This is the only reason for being a MonoBehaviour?
        }

        private IEnumerator WaitForBindComplete()
        {
            while (!m_networkDriver.Bound)
            {
                m_networkDriver.ScheduleUpdate().Complete();
                yield return null; // TODO: Does this not proceed until a client connects as well?
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
    }

    public class RelayUtpSetupHost : RelayUtpSetup
    {
        protected override void JoinRelay()
        {
            RelayInterface.AllocateAsync(m_localLobby.MaxPlayerCount, OnAllocation);
        }

        private void OnAllocation(Allocation allocation)
        {
            RelayInterface.GetJoinCodeAsync(allocation.AllocationId, OnRelayCode);
            BindToAllocation(allocation.RelayServer.IpV4, allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.ConnectionData, allocation.ConnectionData, allocation.Key, 16);
        }

        private void OnRelayCode(string relayCode)
        {
            m_localLobby.RelayCode = relayCode;
            RelayInterface.JoinAsync(m_localLobby.RelayCode, OnJoin);
        }

        private void OnJoin(JoinAllocation joinAllocation)
        {
            m_localLobby.RelayServer = new ServerAddress(joinAllocation.RelayServer.IpV4, joinAllocation.RelayServer.Port);
        }

        protected override void OnBindingComplete()
        {
            if (m_networkDriver.Listen() != 0)
            {
                Debug.LogError("Server failed to listen");
                m_onJoinComplete(false, null);
            }
            else
            {
                Debug.LogWarning("Server is now listening!");
                m_isRelayConnected = true;
                RelayUtpHost host = gameObject.AddComponent<RelayUtpHost>();
                host.Initialize(m_networkDriver, m_connections, m_localUser, m_localLobby);
                m_onJoinComplete(true, host);
            }
        }
    }

    public class RelayUtpSetupClient : RelayUtpSetup
    {
        protected override void JoinRelay()
        {
            m_localLobby.onChanged += OnLobbyChange;
        }

        private void OnLobbyChange(LocalLobby lobby)
        {
            if (m_localLobby.RelayCode != null)
            {
                RelayInterface.JoinAsync(m_localLobby.RelayCode, OnJoin);
                m_localLobby.onChanged -= OnLobbyChange;
            }
        }

        private void OnJoin(JoinAllocation allocation)
        {
            if (allocation == null)
                return; // TODO: Error messaging.
            BindToAllocation(allocation.RelayServer.IpV4, allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.ConnectionData, allocation.HostConnectionData, allocation.Key, 1);
            m_localLobby.RelayServer = new ServerAddress(allocation.RelayServer.IpV4, allocation.RelayServer.Port);
        }

        protected override void OnBindingComplete()
        {
            StartCoroutine(ConnectToServer());
        }

        private IEnumerator ConnectToServer()
        {
            // Once the client is bound to the Relay server, you can send a connection request
            m_connections.Add(m_networkDriver.Connect(m_endpointForServer));
            while (m_networkDriver.GetConnectionState(m_connections[0]) == NetworkConnection.State.Connecting)
            {
                m_networkDriver.ScheduleUpdate().Complete();
                yield return null;
            }
            if (m_networkDriver.GetConnectionState(m_connections[0]) != NetworkConnection.State.Connected)
            {
                Debug.LogError("Client failed to connect to server");
                m_onJoinComplete(false, null);
            }
            else
            {
                RelayUtpClient watcher = gameObject.AddComponent<RelayUtpClient>();
                watcher.Initialize(m_networkDriver, m_connections, m_localUser, m_localLobby);
                m_onJoinComplete(true, watcher);
            }
        }
    }
}
