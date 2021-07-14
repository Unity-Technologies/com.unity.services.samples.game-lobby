using LobbyRelaySample;
using System.Collections;
using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Allocations;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace LobbyRelaySample.Relay
{
    /// <summary>
    /// Responsible for setting up a connection with Relay using UTP, for the lobby host.
    /// Must be a MonoBehaviour since the binding process doesn't have asynchronous callback options.
    /// </summary>
    public abstract class RelayUTPSetup : MonoBehaviour
    {
        // TODO: Eh, don't need to live here.
        unsafe protected static RelayAllocationId ConvertFromAllocationIdBytes(byte[] allocationIdBytes)
        {
            fixed (byte* ptr = allocationIdBytes)
            {
                return RelayAllocationId.FromBytePointer(ptr, allocationIdBytes.Length);
            }
        }

        unsafe protected static RelayConnectionData ConvertConnectionData(byte[] connectionData)
        {
            fixed (byte* ptr = connectionData)
            {
                return RelayConnectionData.FromBytePointer(ptr, RelayConnectionData.k_Length);
            }
        }

        unsafe protected static RelayHMACKey ConvertFromHMAC(byte[] hmac)
        {
            fixed (byte* ptr = hmac)
            {
                return RelayHMACKey.FromBytePointer(ptr, RelayHMACKey.k_Length);
            }
        }
    }

    public class RelayUtpSetup_Host : RelayUTPSetup
    {
        private LocalLobby m_localLobby;
        private Allocation m_allocation;
        private bool m_isRelayConnected = false;
        public NetworkDriver m_ServerDriver;
        private NativeList<NetworkConnection> m_connections;
        private JobHandle m_updateHandle;

        public void DoRelaySetup(LocalLobby localLobby)
        {
            m_localLobby = localLobby;
            RelayInterface.AllocateAsync(m_localLobby.MaxPlayerCount, OnAllocation);
        }

        public void OnAllocation(Allocation allocation)
        {
            m_allocation = allocation;
            //    RelayInterface.GetJoinCodeAsync(allocation.AllocationId, OnRelayCode);
            //}

            //public void OnRelayCode(string relayCode)
            //{
            //    m_localLobby.RelayCode = relayCode;
            //    RelayInterface.JoinAsync(m_localLobby.RelayCode, OnJoin);
            //}

            //private void OnJoin(JoinAllocation allocation)
            //{

            //    // TODO: Use the ServerAddress?
            //    m_localLobby.RelayServer = new ServerAddress(m_allocation.RelayServer.IpV4, m_allocation.RelayServer.Port);

            NetworkEndPoint serverEndpoint = NetworkEndPoint.Parse(m_allocation.RelayServer.IpV4, (ushort)m_allocation.RelayServer.Port);
            // UTP uses pointers instead of managed arrays for performance reasons, so we use these helper functions to convert them
            RelayAllocationId allocationId = ConvertFromAllocationIdBytes(m_allocation.AllocationIdBytes);
            RelayConnectionData connectionData = ConvertConnectionData(m_allocation.ConnectionData);
            RelayHMACKey key = ConvertFromHMAC(m_allocation.Key);

            var relayServerData = new RelayServerData(ref serverEndpoint, 0, ref allocationId, ref connectionData, ref connectionData, ref key);
            relayServerData.ComputeNewNonce();
            var relayNetworkParameter = new RelayNetworkParameter { ServerData = relayServerData };

            StartCoroutine(ServerBindAndListen(relayNetworkParameter, serverEndpoint));
        }

        private IEnumerator ServerBindAndListen(RelayNetworkParameter relayNetworkParameter, NetworkEndPoint serverEndpoint)
        {
            // Create the NetworkDriver using the Relay parameters
            m_ServerDriver = NetworkDriver.Create(new INetworkParameter[] { relayNetworkParameter });
            m_connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);

            // Bind the NetworkDriver to the local endpoint
            if (m_ServerDriver.Bind(NetworkEndPoint.AnyIpv4) != 0)
            {
                Debug.LogError("Server failed to bind");
            }
            else
            {
                // The binding process is an async operation; wait until bound
                while (!m_ServerDriver.Bound)
                {
                    m_ServerDriver.ScheduleUpdate().Complete();
                    yield return null; // TODO: Does this not proceed until a client connects as well?
                }

                // Once the driver is bound you can start listening for connection requests
                if (m_ServerDriver.Listen() != 0)
                {
                    Debug.LogError("Server failed to listen");
                    yield break;
                }
                else
                {
                    Debug.LogWarning("Server is now listening!");
                    m_isRelayConnected = true;
                }

                //var serverConnection = driver.Connect(serverEndpoint);

                //while (driver.GetConnectionState(serverConnection) == NetworkConnection.State.Connecting)
                //{
                //    driver.ScheduleUpdate().Complete();
                //    yield return null;
                //}
                //Debug.LogWarning("Should be good now?");



                //// This successfully sends data, it seems, though it's just to other clients and not actually to the Relay service.
                //while (true)
                //{
                //    yield return new WaitForSeconds(1);
                //    DataStreamWriter writer = default;
                //    if (driver.BeginSend(serverConnection, out writer) == 0)
                //    {
                //        writer.WriteByte(0);
                //        driver.EndSend(writer);
                //        Debug.LogWarning("Sent a byte");
                //    }
                //}

                RelayInterface.GetJoinCodeAsync(m_allocation.AllocationId, OnRelayCode);
            }
        }

        public void OnRelayCode(string relayCode)
        {
            m_localLobby.RelayCode = relayCode;
            //RelayInterface.JoinAsync(m_localLobby.RelayCode, OnRelayJoined);
        }

        private void OnRelayJoined(JoinAllocation allocation)
        {
            StartCoroutine(DoRelayConnect(allocation));
        }
        private IEnumerator DoRelayConnect(JoinAllocation allocation)
        {
            NetworkEndPoint serverEndpoint = NetworkEndPoint.Parse(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port);
            // UTP uses pointers instead of managed arrays for performance reasons, so we use these helper functions to convert them
            RelayAllocationId allocationId = ConvertFromAllocationIdBytes(allocation.AllocationIdBytes);
            RelayConnectionData connectionData = ConvertConnectionData(allocation.ConnectionData);
            RelayHMACKey key = ConvertFromHMAC(allocation.Key);
            var relayServerData = new RelayServerData(ref serverEndpoint, 0, ref allocationId, ref connectionData, ref connectionData, ref key);
            relayServerData.ComputeNewNonce();
            var relayNetworkParameter = new RelayNetworkParameter { ServerData = relayServerData };
            var driver = NetworkDriver.Create(new INetworkParameter[] { relayNetworkParameter });

            var serverConnection = driver.Connect(serverEndpoint);

            Debug.LogWarning("Trying the relay connection now.");

            while (driver.GetConnectionState(serverConnection) == NetworkConnection.State.Connecting)
            {
                driver.ScheduleUpdate().Complete();
                yield return null;
            }
            Debug.LogWarning("Should be good now?");



            // This successfully sends data, it seems, though it's just to other clients and not actually to the Relay service.
            while (true)
            {
                yield return new WaitForSeconds(1);
                DataStreamWriter writer = default;
                if (driver.BeginSend(serverConnection, out writer) == 0)
                {
                    writer.WriteByte(0);
                    driver.EndSend(writer);
                    Debug.LogWarning("Sent a byte");
                }
            }
        }

        

    struct DriverUpdateJob : IJob
    {
        public NetworkDriver driver;
        public NativeList<NetworkConnection> connections;

        public void Execute()
        {
            // Remove connections which have been destroyed from the list of active connections
            for (int i = 0; i < connections.Length; ++i)
            {
                if (!connections[i].IsCreated)
                {
                    connections.RemoveAtSwapBack(i);
                    // Index i is a new connection since we did a swap back, check it again
                    --i;
                }
            }

            // Accept all new connections
            while (true)
            {
                var con = driver.Accept();
                // "Nothing more to accept" is signaled by returning an invalid connection from accept
                if (!con.IsCreated)
                    break;
                connections.Add(con);
            }
        }
    }

    static NetworkConnection ProcessSingleConnection(NetworkDriver.Concurrent driver, NetworkConnection connection)
    {
        DataStreamReader strm;
        NetworkEvent.Type cmd;
        // Pop all events for the connection
        while ((cmd = driver.PopEventForConnection(connection, out strm)) != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Data)
            {
                // For ping requests we reply with a pong message
                int id = strm.ReadInt();
                // Create a temporary DataStreamWriter to keep our serialized pong message
                if (driver.BeginSend(connection, out var pongData) == 0)
                {
                    pongData.WriteInt(id);
                    // Send the pong message with the same id as the ping
                    driver.EndSend(pongData);
                }
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                // When disconnected we make sure the connection return false to IsCreated so the next frames
                // DriverUpdateJob will remove it
                return default(NetworkConnection);
            }
        }

        return connection;
    }

    struct PongJob : Unity.Jobs.IJobParallelForDefer
    {
        public NetworkDriver.Concurrent driver;
        public NativeArray<NetworkConnection> connections;

        public void Execute(int i)
        {
            connections[i] = ProcessSingleConnection(driver, connections[i]);
        }
    }

    private void Update()
    {
        // When connecting to the relay we need to this? 
        if (m_ServerDriver.IsCreated && !m_isRelayConnected)
        {
            m_ServerDriver.ScheduleUpdate().Complete();
            
            var updateJob = new DriverUpdateJob {driver = m_ServerDriver, connections = m_connections};
            updateJob.Schedule().Complete();
        }
    }

    void LateUpdate()
    {
        // On fast clients we can get more than 4 frames per fixed update, this call prevents warnings about TempJob
        // allocation longer than 4 frames in those cases
        if (m_ServerDriver.IsCreated && m_isRelayConnected)
            m_updateHandle.Complete();
    }

    void FixedUpdate()
    {
        if (m_ServerDriver.IsCreated && m_isRelayConnected) {
            // Wait for the previous frames ping to complete before starting a new one, the Complete in LateUpdate is not
            // enough since we can get multiple FixedUpdate per frame on slow clients
            m_updateHandle.Complete();
            var updateJob = new DriverUpdateJob {driver = m_ServerDriver, connections = m_connections};
            var pongJob = new PongJob
            {
                // PongJob is a ParallelFor job, it must use the concurrent NetworkDriver
                driver = m_ServerDriver.ToConcurrent(),
                // PongJob uses IJobParallelForDeferExtensions, we *must* use AsDeferredJobArray in order to access the
                // list from the job
                connections = m_connections.AsDeferredJobArray()
            };
            // Update the driver should be the first job in the chain
            m_updateHandle = m_ServerDriver.ScheduleUpdate();
            // The DriverUpdateJob which accepts new connections should be the second job in the chain, it needs to depend
            // on the driver update job
            m_updateHandle = updateJob.Schedule(m_updateHandle);
            // PongJob uses IJobParallelForDeferExtensions, we *must* schedule with a list as first parameter rather than
            // an int since the job needs to pick up new connections from DriverUpdateJob
            // The PongJob is the last job in the chain and it must depends on the DriverUpdateJob
            m_updateHandle = pongJob.Schedule(m_connections, 1, m_updateHandle);
        }
        
    }



    }

    public class RelayUtpSetup_Client : RelayUTPSetup
    {
        LocalLobby m_localLobby;

        public void JoinRelay(LocalLobby localLobby)
        {
            m_localLobby = localLobby;
            localLobby.onChanged += OnLobbyChange;
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
            {
                // TODO: Error messaging.
                return;
            }

            // Collect and convert the Relay data from the join response
            var serverEndpoint = NetworkEndPoint.Parse(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port);
            var allocationId = ConvertFromAllocationIdBytes(allocation.AllocationIdBytes);
            var connectionData = ConvertConnectionData(allocation.ConnectionData);
            var hostConnectionData = ConvertConnectionData(allocation.HostConnectionData);
            var key = ConvertFromHMAC(allocation.Key);

            // Prepare the RelayNetworkParameter
            var relayServerData = new RelayServerData(ref serverEndpoint, 0, ref allocationId, ref connectionData, ref hostConnectionData, ref key);
            relayServerData.ComputeNewNonce();

            var relayNetworkParameter = new RelayNetworkParameter { ServerData = relayServerData };
            StartCoroutine(ServerBindAndListen(relayNetworkParameter, serverEndpoint));
        }

        private IEnumerator ServerBindAndListen(RelayNetworkParameter relayNetworkParameter, NetworkEndPoint serverEndpoint)
        {
            var driver = NetworkDriver.Create(new INetworkParameter[] { relayNetworkParameter });

            // Bind the NetworkDriver to the available local endpoint.
            // This will send the bind request to the Relay server
            if (driver.Bind(NetworkEndPoint.AnyIpv4) != 0)
            {
                Debug.LogError("Client failed to bind");
            }
            else
            {
                while (!driver.Bound)
                {
                    driver.ScheduleUpdate().Complete();
                    yield return null;
                }

                // Once the client is bound to the Relay server, you can send a connection request
                var serverConnection = driver.Connect(serverEndpoint);

                while (driver.GetConnectionState(serverConnection) == NetworkConnection.State.Connecting)
                {
                    driver.ScheduleUpdate().Complete();
                    yield return null;
                }

                if (driver.GetConnectionState(serverConnection) != NetworkConnection.State.Connected)
                {
                    Debug.LogError("Client failed to connect to server");
                }
            }
        }
    }
}
