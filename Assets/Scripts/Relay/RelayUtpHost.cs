using Unity.Networking.Transport;
using MsgType = LobbyRelaySample.Relay.RelayUtpSetup.MsgType;

namespace LobbyRelaySample.Relay
{
    public class RelayUtpHost : RelayUtpClient
    {
        protected override void OnUpdate()
        {
            base.OnUpdate();
            DoHeartbeat();
        }

        protected override void ProcessNetworkEventDataAdditional(DataStreamReader strm, NetworkEvent.Type cmd, MsgType msgType, string id)
        {
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
                var con = m_networkDriver.Accept();
                if (!con.IsCreated) // "Nothing more to accept" is signalled by returning an invalid connection from Accept.
                    break;
                m_connections.Add(con);
            }
        }
    }
}
