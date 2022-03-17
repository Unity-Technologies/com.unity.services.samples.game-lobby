using System;
using Unity.Networking.Transport;

namespace LobbyRelaySample.relay
{
    /// <summary>
    /// The Relay host doesn't need to know what might approve or disapprove of a pending connection, so this will
    /// broadcast a message that approval is being sought, and if nothing disapproves, the connection will be permitted.
    /// </summary>
    public class RelayPendingApproval : IDisposable
    {
        private NetworkConnection m_pendingConnection;
        private bool m_hasDisposed = false;
        private const float k_waitTime = 0.1f;
        private Action<NetworkConnection, Approval> m_onResult;
        public string ID { get; private set; }

        public RelayPendingApproval(NetworkConnection conn, Action<NetworkConnection, Approval> onResult, string id)
        {
            m_pendingConnection = conn;
            m_onResult = onResult;
            ID = id;
            Locator.Get.UpdateSlow.Subscribe(Approve, k_waitTime);
            Locator.Get.Messenger.OnReceiveMessage(MessageType.ClientUserSeekingDisapproval, (Action<Approval>)Disapprove);
        }
        ~RelayPendingApproval() { Dispose(); }

        private void Approve(float unused)
        {
            try
            {   m_onResult?.Invoke(m_pendingConnection, Approval.OK);
            }
            finally
            {   Dispose();
            }
        }

        public void Disapprove(Approval reason)
        {
            try
            {   m_onResult?.Invoke(m_pendingConnection, reason);
            }
            finally
            {   Dispose();
            }
        }

        public void Dispose()
        {
            if (!m_hasDisposed)
            {
                Locator.Get.UpdateSlow.Unsubscribe(Approve);
                m_hasDisposed = true;
            }
        }
    }
}
