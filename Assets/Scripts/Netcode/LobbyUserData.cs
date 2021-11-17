using Unity.Netcode;

namespace LobbyRelaySample.ngo
{
    /// <summary>
    /// An example of a custom type serialized for use in RPC calls.
    /// </summary>
    public struct LobbyUserData : INetworkSerializable
    {
        public string name;
        public ulong id;
        public LobbyUserData(string name, ulong id) { this.name = name; this.id = id; }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref name);
            serializer.SerializeValue(ref id);
        }
    }
}
