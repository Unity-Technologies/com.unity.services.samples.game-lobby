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
        protected LobbyUser m_localUser;
        protected LocalLobby m_localLobby;
        private bool m_hasDisposed = false;
        protected NetworkDriver m_networkDriver;
        protected List<NetworkConnection> m_connections; // TODO: Make it clearer that this is just the one member?

        private bool m_hasSentInitialMessage = false;

        public void Initialize(NetworkDriver networkDriver, List<NetworkConnection> connections, LobbyUser localUser, LocalLobby localLobby)
        {
            m_localUser = localUser;
            m_localLobby = localLobby;
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
            if (m_connections.Count == 0) // Could be the case for the server, should probably actually break that out after all?
                return;
            m_networkDriver.ScheduleUpdate().Complete();
            DoUserUpdate(m_networkDriver, m_connections[0]); // TODO: Hmm...I don't think this ends up working if the host has to manually transmit changes over all connections.
        }

        public void Update()
        {
            OnUpdate();
        }

        protected virtual void OnUpdate()
        {
            m_networkDriver.ScheduleUpdate().Complete();
            ReceiveNetworkEvents(m_networkDriver, m_connections);
            if (!m_hasSentInitialMessage && !(this is RelayHost))
                SendInitialMessage(m_networkDriver, m_connections[0]);
        }

        private void ReceiveNetworkEvents(NetworkDriver driver, List<NetworkConnection> connections) // TODO: Just the one connection. Also not NativeArray.
        {
            DataStreamReader strm;
            NetworkEvent.Type cmd;
            foreach (NetworkConnection connection in connections)
            {
                while ((cmd = connection.PopEvent(driver, out strm)) != NetworkEvent.Type.Empty)
                {
                    ProcessNetworkEvent(strm, cmd);
                }
            }
        }

        protected virtual void ProcessNetworkEvent(DataStreamReader strm, NetworkEvent.Type cmd)
        {
            if (cmd == NetworkEvent.Type.Data)
            {
                MsgType msgType = (MsgType)strm.ReadByte();
                string id = ReadLengthAndString(ref strm);
                if (id == m_localUser.ID || !m_localLobby.LobbyUsers.ContainsKey(id))
                    return;

                if (msgType == MsgType.PlayerName)
                {
                    string name = ReadLengthAndString(ref strm);
                    m_localLobby.LobbyUsers[id].DisplayName = name;
                    Debug.LogError("User id " + id + " is named " + name);
                }
                else if (msgType == MsgType.Emote)
                {
                    EmoteType emote = (EmoteType)strm.ReadByte();
                    m_localLobby.LobbyUsers[id].Emote = emote;
                    Debug.LogError("User id " + id + " has emote " + emote.ToString());
                }
                else if (msgType == MsgType.ReadyState)
                {
                    UserStatus status = (UserStatus)strm.ReadByte();
                    m_localLobby.LobbyUsers[id].UserStatus = status;
                    Debug.LogError("User id " + id + " has state " + status.ToString());
                }
            }
        }

        unsafe private string ReadLengthAndString(ref DataStreamReader strm)
        {
            byte length = strm.ReadByte();
            byte[] bytes = new byte[length];
            fixed (byte* ptr = bytes)
            {
                strm.ReadBytes(ptr, length);
            }
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        private void SendInitialMessage(NetworkDriver driver, NetworkConnection connection)
        {
            // Assuming this is only created after the Relay connection is successful.
            // TODO: Retry logic for that?
            DoUserUpdate(driver, connection);
            m_hasSentInitialMessage = true;
        }

        private void DoUserUpdate(NetworkDriver driver, NetworkConnection connection)
        {
            // TODO: Combine these all into one message, if I'm just going to send them all each time anyway.

            WriteString(driver, connection, MsgType.PlayerName, m_localUser.DisplayName);
            WriteByte(driver, connection, MsgType.Emote, (byte)m_localUser.Emote);
            WriteByte(driver, connection, MsgType.ReadyState, (byte)m_localUser.UserStatus);
        }

        // TODO: We do have a character limit on the name entry field, right?

        // Msg type, ID length, ID, str length, str
        // Not doing bit packing.
        private void WriteString(NetworkDriver driver, NetworkConnection connection, MsgType msgType, string str)
        {
            byte[] idBytes = System.Text.Encoding.UTF8.GetBytes(m_localUser.ID);
            byte[] strBytes = System.Text.Encoding.UTF8.GetBytes(str);

            List<byte> message = new List<byte>(idBytes.Length + strBytes.Length + 3);
            message.Add((byte)msgType);
            message.Add((byte)idBytes.Length);
            message.AddRange(idBytes);
            message.Add((byte)strBytes.Length);
            message.AddRange(strBytes);

            if (driver.BeginSend(connection, out var dataStream) == 0) // Oh, should check this first?
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

        private void WriteByte(NetworkDriver driver, NetworkConnection connection, MsgType msgType, byte value)
        {
            byte[] idBytes = System.Text.Encoding.UTF8.GetBytes(m_localUser.ID);
            List<byte> message = new List<byte>(idBytes.Length + 3);
            message.Add((byte)msgType);
            message.Add((byte)idBytes.Length);
            message.AddRange(idBytes);
            message.Add(value);

            if (driver.BeginSend(connection, out var dataStream) == 0) // Oh, should check this first?
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
    }
}
