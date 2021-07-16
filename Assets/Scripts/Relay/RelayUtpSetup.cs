using LobbyRelaySample;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
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
    public abstract class RelayUTPSetup : MonoBehaviour
    {
        protected bool m_isRelayConnected = false;
        protected NetworkDriver m_networkDriver;
        protected NativeList<NetworkConnection> m_connections;
        protected NetworkEndPoint m_endpointForServer;
        protected JobHandle m_currentUpdateHandle;
        protected LocalLobby m_localLobby;
        protected Action<bool, string> m_onJoinComplete;

        protected enum MsgType { NewPlayer = 0, PingPong = 1, ReadyState = 2, PlayerName = 3, Emote = 4 } // We only use 3 bits for this.

        public void BeginRelayJoin(LocalLobby localLobby)//, Action<bool, string> onJoinComplete)
        {
            m_localLobby = localLobby;
//            m_onJoinComplete = onJoinComplete;
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
            m_connections = new NativeList<NetworkConnection>(connectionCapacity, Allocator.Persistent);

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


        private void LateUpdate()
        {
            if (m_networkDriver.IsCreated && m_isRelayConnected)
                m_currentUpdateHandle.Complete(); // This prevents warnings about a job allocation longer than 4 frames if FixedUpdate is very fast.
        }
    }

    public class RelayUtpSetup_Host : RelayUTPSetup
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
            }
            else
            {
                Debug.LogWarning("Server is now listening!");
                m_isRelayConnected = true;
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

        private struct PongJob : IJobParallelForDefer
        {
            public NetworkDriver.Concurrent driver;
            public NativeArray<NetworkConnection> connections;

            public void Execute(int i)
            {
                DataStreamReader strm;
                NetworkEvent.Type cmd;
                // Pop all events for the connection
                while ((cmd = driver.PopEventForConnection(connections[i], out strm)) != NetworkEvent.Type.Empty)
                {
                    if (cmd == NetworkEvent.Type.Connect)
                    {
                        // TODO: Assuming that i is the index in connections, which will be the order in which they are received and also will not shift downward if disconnects happen? Need to test with multiple clients.
                        SendPong(driver, connections);
                    }
                    else if (cmd == NetworkEvent.Type.Data)
                    {
                        byte header = strm.ReadByte();
                        int contents = header % 32;
                        header = (byte)(header >> 5);
                        MsgType msgType = (MsgType)header;

                        if (msgType == MsgType.PingPong)
                        {
                            SendPong(driver, connections);
                        }
                        else if (msgType == MsgType.PlayerName)
                        {
                            byte[] nameBytes = new byte[contents];
                            unsafe
                            {
                                fixed(byte* namePtr = nameBytes)
                                {
                                    strm.ReadBytes(namePtr, contents);
                                }
                            }
                            string name = System.Text.Encoding.UTF8.GetString(nameBytes);
                            Debug.LogWarning("Received name for connection " + i + ": " + name);


                            SendPong(driver, connections);
                        }
                    }
                    else if (cmd == NetworkEvent.Type.Disconnect)
                    {
                        // When disconnected we make sure the connection return false to IsCreated so the next frames
                        // DriverUpdateJob will remove it
                        connections[i] = default(NetworkConnection);
                    }
                }

                void SendPong(NetworkDriver.Concurrent driver, NativeArray<NetworkConnection> connections)
                {
                    byte reply = (byte)(((int)MsgType.PingPong) << 5);
                    if (driver.BeginSend(connections[i], out var writeData) == 0)
                    {
                        writeData.WriteByte(reply);
                        driver.EndSend(writeData);
                        Debug.LogWarning("Sent pong for connection " + i);
                    }
                }
            }
        }

        private void Update()
        {
            // When connecting to the relay we need to this? 
            if (m_networkDriver.IsCreated && !m_isRelayConnected)
            {
                m_networkDriver.ScheduleUpdate().Complete();
            
                var updateJob = new DriverUpdateJob {driver = m_networkDriver, connections = m_connections};
                updateJob.Schedule().Complete();
            }
        }

        void FixedUpdate()
        {
            if (m_networkDriver.IsCreated && m_isRelayConnected) {
                // Wait for the previous frames ping to complete before starting a new one, the Complete in LateUpdate is not
                // enough since we can get multiple FixedUpdate per frame on slow clients
                m_currentUpdateHandle.Complete();
                var updateJob = new DriverUpdateJob {driver = m_networkDriver, connections = m_connections};
                var pongJob = new PongJob
                {
                    // PongJob is a ParallelFor job, it must use the concurrent NetworkDriver
                    driver = m_networkDriver.ToConcurrent(),
                    // PongJob uses IJobParallelForDeferExtensions, we *must* use AsDeferredJobArray in order to access the
                    // list from the job
                    connections = m_connections.AsDeferredJobArray()
                };
                // Update the driver should be the first job in the chain
                m_currentUpdateHandle = m_networkDriver.ScheduleUpdate();
                // The DriverUpdateJob which accepts new connections should be the second job in the chain, it needs to depend
                // on the driver update job
                m_currentUpdateHandle = updateJob.Schedule(m_currentUpdateHandle);
                // PongJob uses IJobParallelForDeferExtensions, we *must* schedule with a list as first parameter rather than
                // an int since the job needs to pick up new connections from DriverUpdateJob
                // The PongJob is the last job in the chain and it must depends on the DriverUpdateJob
                m_currentUpdateHandle = pongJob.Schedule(m_connections, 1, m_currentUpdateHandle);
            }
        }
    }

    public class RelayUtpSetup_Client : RelayUTPSetup
    {
        // TEMP
        public string myName { private get; set; }


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
                Debug.LogError("Client failed to connect to server");
        }

        private struct PingJob : IJob
        {
            public NetworkDriver driver;
            public NativeArray<NetworkConnection> connection; // TODO: I think we were using NativeArray to merely contain one entry, since we'd be unable to pass just that via jobs?
            public float fixedTime;
            public NativeArray<byte> myName;

            public void Execute()
            {
                DataStreamReader strm;
                NetworkEvent.Type cmd;
                // Process all events on the connection. If the connection is invalid it will return Empty immediately
                while (connection.Length > 0 && (cmd = connection[0].PopEvent(driver, out strm)) != NetworkEvent.Type.Empty)
                {
                    if (cmd == NetworkEvent.Type.Connect)
                    {
                        // Same as name sending.
                        if (myName == null || myName.Length == 0)
                            return;
                        List<byte> message = new List<byte>(myName.Length + 1);
                        message.AddRange(myName);
                        byte header = (byte) ((((int)MsgType.PlayerName) << 5) + myName.Length); // TODO: Truncate length;
                        message.Insert(0, header);

                        if (driver.BeginSend(connection[0], out var connectData) == 0) // Oh, should check this first?
                        {
                            byte[] bytes = message.ToArray();
                            unsafe
                            {
                                fixed (byte* bytesPtr = bytes)
                                {
                                    connectData.WriteBytes(bytesPtr, message.Count);
                                    driver.EndSend(connectData);
                                }
                            }
                        }
                    }
                    else if (cmd == NetworkEvent.Type.Data)
                    {
                        //// When the pong message is received we calculate the ping time and disconnect
                        //pingStats[1] = (int)((fixedTime - pendingPings[0].time) * 1000);
                        //connection[0].Disconnect(driver);
                        //connection[0] = default(NetworkConnection);
                        if (driver.BeginSend(connection[0], out var pingData) == 0)
                        {
                            pingData.WriteInt(1234);
                            driver.EndSend(pingData);
                        }
                    }
                    else if (cmd == NetworkEvent.Type.Disconnect)
                    {
                        // If the server disconnected us we clear out connection
                        connection[0] = default(NetworkConnection);
                    }
                }
            }
        }

        private void Update()
        {
            // When connecting to the relay we need to this? 
            if (m_networkDriver.IsCreated && !m_isRelayConnected)
            {
                m_networkDriver.ScheduleUpdate().Complete();

                var pingJob = new PingJob
                {
                    driver = m_networkDriver,
                    connection = m_connections.AsArray(),
                    fixedTime = Time.fixedTime,
                    myName = new NativeArray<byte>(System.Text.Encoding.UTF8.GetBytes(myName), Allocator.TempJob)
                };

                pingJob.Schedule().Complete();
            }
        }

        void FixedUpdate()
        {
            if (m_networkDriver.IsCreated && m_isRelayConnected)
            {

                // Wait for the previous frames ping to complete before starting a new one, the Complete in LateUpdate is not
                // enough since we can get multiple FixedUpdate per frame on slow clients
                m_currentUpdateHandle.Complete();

                var pingJob = new PingJob
                {
                    driver = m_networkDriver,
                    connection = m_connections,
                    fixedTime = Time.fixedTime,
                    myName = new NativeArray<byte>(System.Text.Encoding.UTF8.GetBytes(myName), Allocator.TempJob)
                };
                // Schedule a chain with the driver update followed by the ping job
                m_currentUpdateHandle = m_networkDriver.ScheduleUpdate();
                m_currentUpdateHandle = pingJob.Schedule(m_currentUpdateHandle);
            }
        }
    }
}
