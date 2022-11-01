using System;

namespace LobbyRelaySample
{
    /// <summary>
    /// Just for displaying the anonymous Relay IP.
    /// </summary>
    public class ServerAddress : IEquatable<ServerAddress>
    {
        string m_IP;
        int m_Port;

        public string IP => m_IP;
        public int Port => m_Port;

        public ServerAddress(string ip, int port)
        {
            m_IP = ip;
            m_Port = port;
        }

        public override string ToString()
        {
            return $"{m_IP}:{m_Port}";
        }

        public bool Equals(ServerAddress other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return m_IP == other.m_IP && m_Port == other.m_Port;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ServerAddress)obj);
        }

    }
}