using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;
using MsgType = LobbyRelaySample.Relay.RelayUtpSetup.MsgType;

namespace LobbyRelaySample.Relay
{
    /// <summary>
    /// This will handle observing the local player and updating remote players over Relay when there are local changes.
    /// Created after the connection to Relay has been confirmed.
    /// </summary>
    public class RelayUtpClient : MonoBehaviour // This is a MonoBehaviour merely to have access to Update.
    {
        protected LobbyUser m_localUser;
        protected LocalLobby m_localLobby;
        protected NetworkDriver m_networkDriver;
        protected List<NetworkConnection> m_connections; // For clients, this has just one member, but for hosts it will have more.

        private bool m_hasSentInitialMessage = false;

        public void Initialize(NetworkDriver networkDriver, List<NetworkConnection> connections, LobbyUser localUser, LocalLobby localLobby)
        {
            m_localUser = localUser;
            m_localLobby = localLobby;
            m_localUser.onChanged += OnLocalChange;
            m_networkDriver = networkDriver;
            m_connections = connections;
            Locator.Get.UpdateSlow.Subscribe(UpdateSlow);

            if (this is RelayUtpHost) // The host will be alone in the lobby at first, so they need not send any messages right away.
                m_hasSentInitialMessage = true;
        }
        public void OnDestroy()
        {
            m_localUser.onChanged -= OnLocalChange;
            Locator.Get.UpdateSlow.Unsubscribe(UpdateSlow);
        }

        private void OnLocalChange(LobbyUser localUser)
        {
            if (m_connections.Count == 0) // This could be the case for the host alone in the lobby.
                return;
            m_networkDriver.ScheduleUpdate().Complete();
            foreach (NetworkConnection conn in m_connections)
                DoUserUpdate(m_networkDriver, conn); // TODO: Hmm...I don't think this ends up working if the host has to manually transmit changes over all connections.
        }

        public void Update()
        {
            OnUpdate();
        }

        private void UpdateSlow(float dt)
        {
            // Clients need to send any data over UTP periodically, or else the connection will timeout.
            foreach (NetworkConnection connection in m_connections)
                WriteByte(m_networkDriver, connection, "0", MsgType.Ping, 0); // The ID doesn't matter here, so send a minimal number of bytes.
        }

        protected virtual void OnUpdate()
        {
            m_networkDriver.ScheduleUpdate().Complete(); // This pumps all messages, which pings the Relay allocation and keeps it alive.
            ReceiveNetworkEvents(m_networkDriver, m_connections);
            if (!m_hasSentInitialMessage)
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
                    ProcessNetworkEvent(connection, strm, cmd);
                }
            }
        }

        private void ProcessNetworkEvent(NetworkConnection conn, DataStreamReader strm, NetworkEvent.Type cmd)
        {
            if (cmd == NetworkEvent.Type.Data)
            {
                MsgType msgType = (MsgType)strm.ReadByte();
                string id = ReadLengthAndString(ref strm);
                if (id == m_localUser.ID || !m_localLobby.LobbyUsers.ContainsKey(id)) // TODO: Do we want to hold onto the message if the user isn't present *now* in case they're pending?
                    return;

                if (msgType == MsgType.PlayerName)
                {
                    string name = ReadLengthAndString(ref strm);
                    m_localLobby.LobbyUsers[id].DisplayName = name;
                }
                else if (msgType == MsgType.Emote)
                {
                    EmoteType emote = (EmoteType)strm.ReadByte();
                    m_localLobby.LobbyUsers[id].Emote = emote;
                }
                else if (msgType == MsgType.ReadyState)
                {
                    UserStatus status = (UserStatus)strm.ReadByte();
                    m_localLobby.LobbyUsers[id].UserStatus = status;
                }
                ProcessNetworkEventDataAdditional(conn, strm, msgType, id);
            }
            ProcessNetworkEventAdditional(strm, cmd);
        }

        protected virtual void ProcessNetworkEventAdditional(DataStreamReader strm, NetworkEvent.Type cmd) { }

        protected virtual void ProcessNetworkEventDataAdditional(NetworkConnection conn, DataStreamReader strm, MsgType msgType, string id) { }

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
            DoUserUpdate(driver, connection); // Assuming this is only created after the Relay connection is successful.
            m_hasSentInitialMessage = true;
        }

        private void DoUserUpdate(NetworkDriver driver, NetworkConnection connection)
        {
            // Only update with actual changes. (If multiple change at once, we send messages for each separately, but that shouldn't happen often.)
            if (0 < (m_localUser.LastChanged & LobbyUser.UserMembers.DisplayName))
                WriteString(driver, connection, m_localUser.ID, MsgType.PlayerName, m_localUser.DisplayName);
            if (0 < (m_localUser.LastChanged & LobbyUser.UserMembers.Emote))
                WriteByte(driver, connection, m_localUser.ID, MsgType.Emote, (byte)m_localUser.Emote);
            if (0 < (m_localUser.LastChanged & LobbyUser.UserMembers.UserStatus))
                WriteByte(driver, connection, m_localUser.ID, MsgType.ReadyState, (byte)m_localUser.UserStatus);
        }

        /// <summary>
        /// Write string data as: [1 byte: msgType][1 byte: id length N][N bytes: id][1 byte: string length M][M bytes: string]
        /// </summary>
        protected void WriteString(NetworkDriver driver, NetworkConnection connection, string id, MsgType msgType, string str)
        {
            byte[] idBytes = System.Text.Encoding.UTF8.GetBytes(id);
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

        /// <summary>
        /// Write byte data as: [1 byte: msgType][1 byte: id length N][N bytes: id][1 byte: data]
        /// </summary>
        protected void WriteByte(NetworkDriver driver, NetworkConnection connection, string id, MsgType msgType, byte value)
        {
            byte[] idBytes = System.Text.Encoding.UTF8.GetBytes(id);
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
