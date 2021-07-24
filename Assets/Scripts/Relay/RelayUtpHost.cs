using Unity.Networking.Transport;
using MsgType = LobbyRelaySample.Relay.RelayUtpSetup.MsgType;

namespace LobbyRelaySample.Relay
{
    /// <summary>
    /// In addition to maintaining a heartbeat with the Relay server to keep it from timing out, the host player must pass network events
    /// from clients to all other clients, since they don't connect to each other.
    /// </summary>
    public class RelayUtpHost : RelayUtpClient
    {
        protected override void OnUpdate()
        {
            base.OnUpdate();
            DoHeartbeat();
        }

        /// <summary>
        /// When a new client connects, they need to be given all up-to-date info.
        /// </summary>
        private void OnNewConnection(NetworkConnection conn)
        {
            // When a new client connects, they need to be updated with the current state of everyone else.
            // (We can't exclude this client from the events we send to it, since we don't have its ID in strm, but it will ignore messages about itself on arrival.)
            foreach (var user in m_localLobby.LobbyUsers)
                ForceFullUserUpdate(m_networkDriver, conn, user.Value);
        }

        protected override void ProcessNetworkEventDataAdditional(NetworkConnection conn, DataStreamReader strm, MsgType msgType, string id)
        {
            if (msgType == MsgType.PlayerName)
            {
                string name = m_localLobby.LobbyUsers[id].DisplayName;
                foreach (NetworkConnection otherConn in m_connections)
                {
                    if (otherConn == conn)
                        continue;
                    WriteString(m_networkDriver, otherConn, id, msgType, name);
                }
            }
            else if (msgType == MsgType.Emote || msgType == MsgType.ReadyState)
            {
                byte value = msgType == MsgType.Emote ? (byte)m_localLobby.LobbyUsers[id].Emote : (byte)m_localLobby.LobbyUsers[id].UserStatus;
                foreach (NetworkConnection otherConn in m_connections)
                {
                    if (otherConn == conn)
                        continue;
                    WriteByte(m_networkDriver, otherConn, id, msgType, value);
                }
            }

            // Note that the strm contents might have already been consumed, depending on the msgType.
            if (msgType == MsgType.ReadyState)
            {
                // TODO: Check if all players have readied.
            }
        }

        /// <summary>
        /// Clean out destroyed connections, and accept all new ones.
        /// </summary>
        private void DoHeartbeat()
        {
            m_networkDriver.ScheduleUpdate().Complete();

            for (int c = m_connections.Count - 1; c >= 0; c--)
            {
                if (!m_connections[c].IsCreated)
                    m_connections.RemoveAt(c);
            }
            while (true)
            {
                var conn = m_networkDriver.Accept();
                if (!conn.IsCreated) // "Nothing more to accept" is signalled by returning an invalid connection from Accept.
                    break;
                m_connections.Add(conn);
                OnNewConnection(conn);
            }
        }
    }
}
