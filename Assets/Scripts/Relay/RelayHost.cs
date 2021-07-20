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
    public class RelayHost : MonoBehaviour // TODO: Should it be a child of the client version?
    {
        private int m_userIdNext = 0;
        private LocalLobby m_localLobby;
        private Dictionary<int, LobbyUser> m_usersById = new Dictionary<int, LobbyUser>();
        protected NetworkDriver m_networkDriver;
        protected NativeList<NetworkConnection> m_connections;

        public void Initialize(NetworkDriver networkDriver, NativeList<NetworkConnection> connections, LocalLobby localLobby)
        {
            m_networkDriver = networkDriver;
            m_connections = connections;
            m_localLobby = localLobby;
        }

        public void Update()
        {
            DoHeartbeat();
            ReceiveNetworkEvents(m_networkDriver.ToConcurrent(), m_connections);
        }

        private void ReceiveNetworkEvents(NetworkDriver.Concurrent driver, NativeArray<NetworkConnection> connections) // TODO: Need the concurrent?
        {
            DataStreamReader strm;
            NetworkEvent.Type cmd;
            foreach (NetworkConnection connection in connections)
            {
                while ((cmd = driver.PopEventForConnection(connection, out strm)) != NetworkEvent.Type.Empty)
                {
                    if (cmd == NetworkEvent.Type.Connect)
                    {

                    }
                    else if (cmd == NetworkEvent.Type.Data)
                    {
                        // TODO: Update other users' data, with a shared mechanism with servers.
                        
                        byte header = strm.ReadByte();
                        MsgType msgType = (MsgType)(header % 8);
                        header = (byte)(header >> 3);
                        int playerId = header;

                        if (msgType == MsgType.NewPlayer)
                        {
                            byte length = strm.ReadByte();
                            byte[] idBytes = new byte[length];
                            unsafe
                            {
                                fixed(byte* idPtr = idBytes)
                                {
                                    strm.ReadBytes(idPtr, length);
                                }
                            }
                            string id = System.Text.Encoding.UTF8.GetString(idBytes);
                            Debug.LogWarning("Received ID: " + id);

                            if (playerId == 0)
                            {
                                // New player, which we could detect in the Connect event but only insofar as the connection is associated with it, but we need to find the LobbyUser.
                                foreach (var user in m_localLobby.LobbyUsers)
                                {
                                    if (user.Value.ID != id)
                                        continue;
                                    int idShort = ++m_userIdNext;
                                    m_usersById.Add(idShort, user.Value);
                                    playerId = idShort;
                                    break;
                                }
                            }
                            Debug.LogWarning("Providing a player ID of " + ((byte)playerId));
                            SendMessageBytes(m_networkDriver.ToConcurrent(), connection, GetHeaderByte(MsgType.ProvidePlayerId), (byte)playerId);
                        }
                    }
                }
            }
        }

        private void DoHeartbeat()
        {
            // Update the driver should be the first job in the chain
            m_networkDriver.ScheduleUpdate().Complete();
            // Remove connections which have been destroyed from the list of active connections
            for (int c = m_connections.Length - 1; c >= 0; c--)
            {
                if (!m_connections[c].IsCreated)
                    m_connections.RemoveAtSwapBack(c);
            }

            // Accept all new connections
            while (true)
            {
                var con = m_networkDriver.Accept();
                // "Nothing more to accept" is signaled by returning an invalid connection from accept
                if (!con.IsCreated)
                    break;
                m_connections.Add(con);
            }
        }

        private byte GetHeaderByte(MsgType msgType)
        {
            return (byte)msgType; // The host doesn't have a player ID.
        }

        unsafe private void SendMessageBytes(NetworkDriver.Concurrent driver, NetworkConnection connection, params byte[] msg)
        {
            if (driver.BeginSend(connection, out var writeData) == 0)
            {
                fixed (byte* msgPtr = msg)
                {
                    writeData.WriteBytes(msgPtr, msg.Length);
                    driver.EndSend(writeData);
                }
            }
        }
    }
}
