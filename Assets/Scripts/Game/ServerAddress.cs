using UnityEngine;

namespace LobbyRelaySample
{
    /// <summary>
    /// This is where your netcode would go, if you had it.
    /// </summary>
    public class ServerAddress
    {
        string m_IP;
        int m_Port;

        public ServerAddress(string ip, int port)
        {
            m_IP = ip;
            m_Port = port;
            Debug.Log($"Connected To Game Server: {ip}:{port}");
        }

        public override string ToString()
        {
            return $"{m_IP}:{m_Port}";
        }
    }
}
