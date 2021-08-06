using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;
using MsgType = LobbyRelaySample.Relay.RelayUtpSetup.MsgType;

namespace LobbyRelaySample.Relay
{
    /// <summary>
    /// This observes the local player and updates remote players over Relay when there are local changes, demonstrating basic data transfer over the Unity Transport (UTP).
    /// Created after the connection to Relay has been confirmed.
    /// </summary>
    public class RelayUtpClient : MonoBehaviour // This is a MonoBehaviour merely to have access to Update.
    {
        protected LobbyUser m_localUser;
        protected LocalLobby m_localLobby;
        protected NetworkDriver m_networkDriver;
        protected List<NetworkConnection> m_connections; // For clients, this has just one member, but for hosts it will have more.

        protected bool m_hasSentInitialMessage = false;

        public virtual void Initialize(NetworkDriver networkDriver, List<NetworkConnection> connections, LobbyUser localUser, LocalLobby localLobby)
        {
            m_localUser = localUser;
            m_localLobby = localLobby;
            m_localUser.onChanged += OnLocalChange;
            m_networkDriver = networkDriver;
            m_connections = connections;
            Locator.Get.UpdateSlow.Subscribe(UpdateSlow);
        }
        protected virtual void Uninitialize()
        {
            m_localUser.onChanged -= OnLocalChange;
            Leave();
            Locator.Get.UpdateSlow.Unsubscribe(UpdateSlow);
        }
        public void OnDestroy()
        {
            Uninitialize();
        }

        private void OnLocalChange(LobbyUser localUser)
        {
            if (m_connections.Count == 0) // This could be the case for the host alone in the lobby.
                return;
            m_networkDriver.ScheduleUpdate().Complete();
            foreach (NetworkConnection conn in m_connections)
                DoUserUpdate(m_networkDriver, conn, m_localUser);
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

        private void ReceiveNetworkEvents(NetworkDriver driver, List<NetworkConnection> connections)
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
                if (id == m_localUser.ID || !m_localLobby.LobbyUsers.ContainsKey(id)) // We don't hold onto messages, since an incoming user will be fully initialized before they send events.
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
                else if (msgType == MsgType.StartCountdown)
                    Locator.Get.Messenger.OnReceiveMessage(MessageType.StartCountdown, null);
                else if (msgType == MsgType.CancelCountdown)
                    Locator.Get.Messenger.OnReceiveMessage(MessageType.CancelCountdown, null);
                else if (msgType == MsgType.ConfirmInGame)
                    Locator.Get.Messenger.OnReceiveMessage(MessageType.ConfirmInGameState, null);
                else if (msgType == MsgType.EndInGame)
                    Locator.Get.Messenger.OnReceiveMessage(MessageType.EndGame, null);

                ProcessNetworkEventDataAdditional(conn, strm, msgType, id);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
                ProcessDisconnectEvent(conn, strm);
        }

        protected virtual void ProcessNetworkEventDataAdditional(NetworkConnection conn, DataStreamReader strm, MsgType msgType, string id) { }

        protected virtual void ProcessDisconnectEvent(NetworkConnection conn, DataStreamReader strm)
        {
            // The host disconnected, and Relay does not support host migration. So, all clients should disconnect.
            Debug.LogError("Host disconnected! Leaving the lobby.");
            Leave();
            Locator.Get.Messenger.OnReceiveMessage(MessageType.ChangeGameState, GameState.JoinMenu);
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
            WriteByte(driver, connection, m_localUser.ID, MsgType.NewPlayer, 0);
            ForceFullUserUpdate(driver, connection, m_localUser); // Assuming this is only created after the Relay connection is successful.
            m_hasSentInitialMessage = true;
        }

        private void DoUserUpdate(NetworkDriver driver, NetworkConnection connection, LobbyUser user)
        {
            // Only update with actual changes. (If multiple change at once, we send messages for each separately, but that shouldn't happen often.)
            if (0 < (user.LastChanged & LobbyUser.UserMembers.DisplayName))
                WriteString(driver, connection, user.ID, MsgType.PlayerName, user.DisplayName);
            if (0 < (user.LastChanged & LobbyUser.UserMembers.Emote))
                WriteByte(driver, connection, user.ID, MsgType.Emote, (byte)user.Emote);
            if (0 < (user.LastChanged & LobbyUser.UserMembers.UserStatus))
                WriteByte(driver, connection, user.ID, MsgType.ReadyState, (byte)user.UserStatus);
        }
        protected void ForceFullUserUpdate(NetworkDriver driver, NetworkConnection connection, LobbyUser user)
        {
            // Note that it would be better to send a single message with the full state, but for the sake of shorter code we'll leave that out here.
            WriteString(driver, connection, user.ID, MsgType.PlayerName, user.DisplayName);
            WriteByte(driver, connection, user.ID, MsgType.Emote, (byte)user.Emote);
            WriteByte(driver, connection, user.ID, MsgType.ReadyState, (byte)user.UserStatus);
        }

        /// <summary>
        /// Write string data as: [1 byte: msgType] [1 byte: id length N] [N bytes: id] [1 byte: string length M] [M bytes: string]
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
        /// Write byte data as: [1 byte: msgType] [1 byte: id length N] [N bytes: id] [1 byte: data]
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

        public void Leave()
        {
            foreach (NetworkConnection connection in m_connections)
                connection.Disconnect(m_networkDriver);
            m_localLobby.RelayServer = null;
        }
    }
}
