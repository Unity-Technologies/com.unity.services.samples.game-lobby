using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

using MsgType = LobbyRelaySample.Relay.RelayUTPSetup.MsgType;

namespace LobbyRelaySample.Relay
{
    /// <summary>
    /// This will handle observing the local user and updating remote users over Relay when there are local changes.
    /// Created after the connection to Relay has been confirmed.
    /// </summary>
    public class RelayUserWatcher : MonoBehaviour, IDisposable
    {
        private LobbyUser m_localUser;
        private bool m_hasDisposed = false;
        private NetworkDriver m_networkDriver;
        private NativeList<NetworkConnection> m_connections; // TODO: Make it clearer that this is just the one member?
        private JobHandle m_mostRecentJob;

        private int? m_playerId = null; // Provided by the host.
        private bool m_hasSentInitialMessage = false;

        public void Initialize(NetworkDriver networkDriver, NativeList<NetworkConnection> connections, LobbyUser localUser)
        {
            m_localUser = localUser;
            m_localUser.onChanged += OnLocalChange; // TODO: This should break up the state type?
            m_networkDriver = networkDriver;
            m_connections = connections;
        }
        public void Dispose()
        {
            if (!m_hasDisposed)
            {
                m_localUser.onChanged -= OnLocalChange;
                m_hasDisposed = true;
            }
        }
        ~RelayUserWatcher() { Dispose(); } // TODO: Disposable or MonoBehaviour?

        private void OnLocalChange(LobbyUser localUser)
        {
            //if (!m_mostRecentJob.IsCompleted)
            //    m_mostRecentJob.Complete();

            m_networkDriver.ScheduleUpdate().Complete();

            DoUserUpdate(m_networkDriver, m_connections);

            //UserUpdateJob userUpdateJob = new UserUpdateJob
            //{ 
            //    driver = m_networkDriver,
            //    connection = m_connections.AsArray(),
            //    myName = new NativeArray<byte>(System.Text.Encoding.UTF8.GetBytes(m_localUser.DisplayName), Allocator.TempJob)
            //};
            //m_mostRecentJob = userUpdateJob.Schedule();

            // TODO: Force complete on disconnect
        }

        public void Update()
        {
            m_networkDriver.ScheduleUpdate().Complete();
            ReceiveNetworkEvents(m_networkDriver, m_connections);
            if (!m_hasSentInitialMessage)
                SendInitialMessage(m_networkDriver, m_connections);
        }

        private void ReceiveNetworkEvents(NetworkDriver driver, NativeArray<NetworkConnection> connection) // TODO: Just the one connection.
        {
            DataStreamReader strm;
            NetworkEvent.Type cmd;
            while (connection.Length > 0 && (cmd = connection[0].PopEvent(driver, out strm)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    // TODO: Update other users' data, with a shared mechanism with servers.

                    byte header = strm.ReadByte();
                    MsgType msgType = (MsgType)(header % 8);
                    header = (byte)(header >> 3);
                    int playerId = header;

                    if (msgType == MsgType.ProvidePlayerId)
                    {
                        byte id = strm.ReadByte();
                        m_playerId = id;
                        Debug.LogError("Received an ID! " + id);
                        // Now, we can send all our info.
                        WriteString(driver, connection, MsgType.PlayerName, m_localUser.DisplayName);
                        // TODO: Send all of it.

                    }
                }
            }
        }

        private void SendInitialMessage(NetworkDriver driver, NativeArray<NetworkConnection> connection)
        {
            // Assuming this is only created after the Relay connection is successful.
            // TODO: Retry logic for that?
            WriteString(driver, connection, MsgType.NewPlayer, m_localUser.ID);
            m_hasSentInitialMessage = true;
        }

        private void DoUserUpdate(NetworkDriver driver, NativeArray<NetworkConnection> connection)
        {
            // Process all events on the connection. If the connection is invalid it will return Empty immediately
            //while (connection.Length > 0 && (cmd = connection[0].PopEvent(driver, out strm)) != NetworkEvent.Type.Empty)
            {
                //if (cmd == NetworkEvent.Type.Connect)
                //{
                //    WriteName(driver, connection);
                //}
                // TODO: Update other clients' local data

                //else if (cmd == NetworkEvent.Type.Disconnect)
                //{
                //    // If the server disconnected us we clear out connection
                //    connection[0] = default(NetworkConnection);
                //}
            }
        }

        // 3-bit message type + 5-bit ID length, 1-byte str length, id, msg
        private void WriteString(NetworkDriver driver, NativeArray<NetworkConnection> connection, MsgType msgType, string str)
        {
            byte[] strBytes = System.Text.Encoding.UTF8.GetBytes(str);
            if (strBytes == null || strBytes.Length == 0)
                return;
            byte header = (byte)(((m_playerId ?? 0) << 5) + msgType);
            byte msgLength = (byte)strBytes.Length;                                                     // TODO: We do have a character limit on the name entry field, right?
            List<byte> message = new List<byte>(strBytes.Length + 2);
            message.Add(header);
            message.Add(msgLength);
            message.AddRange(strBytes);

            if (driver.BeginSend(connection[0], out var dataStream) == 0) // Oh, should check this first?
            {
                byte[] bytes = message.ToArray();
                unsafe
                {
                    fixed (byte* bytesPtr = bytes)
                    {
                        dataStream.WriteBytes(bytesPtr, message.Count);
                        driver.EndSend(dataStream);
                    }
                }
            }
        }

        private struct UserUpdateJob : IJob
        {
            public NetworkDriver driver;
            public NativeArray<NetworkConnection> connection; // TODO: I think we were using NativeArray to merely contain one entry, since we'd be unable to pass just that via jobs?
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
                        byte header = (byte) ((((int)RelayUTPSetup.MsgType.PlayerName) << 5) + myName.Length); // TODO: Truncate length.
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
                    else if (cmd == NetworkEvent.Type.Disconnect)
                    {
                        // If the server disconnected us we clear out connection
                        connection[0] = default(NetworkConnection);
                    }
                }
            }
        }
    }
}
