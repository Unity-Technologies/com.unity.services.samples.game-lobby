using Unity.Netcode;

namespace LobbyRelaySample.ngo
{
    /// <summary>
    /// An example of a custom type serialized for use in RPC calls. This represents the state of a player as far as NGO is concerned,
    /// with relevant fields copied in or modified directly.
    /// </summary>
    public class PlayerData : INetworkSerializable
    {
        public string name;
        public ulong id;
        public int score;
        public PlayerData() { } // A default constructor is explicitly required for serialization.
        public PlayerData(string name, ulong id, int score = 0) { this.name = name; this.id = id; this.score = score; }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref name);
            serializer.SerializeValue(ref id);
            serializer.SerializeValue(ref score);
        }
    }
}
