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
    public class RelayHost : RelayUserWatcher
    {
        protected override void OnUpdate()
        {
            base.OnUpdate();
            DoHeartbeat();
        }

        protected override void ProcessNetworkEvent(DataStreamReader strm, NetworkEvent.Type cmd)
        {
            base.ProcessNetworkEvent(strm, cmd);
            // TODO: The only thing this has to care about is if all players have readied up.
        }

        private void DoHeartbeat()
        {
            // Update the driver should be the first job in the chain
            m_networkDriver.ScheduleUpdate().Complete();
            // Remove connections which have been destroyed from the list of active connections
            for (int c = m_connections.Count - 1; c >= 0; c--)
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
    }
}
