using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;



namespace Unity.Services.Relay.Models
{
    /// <summary>
    /// An allocation created via a join code
    /// </summary>
    /// <param name="allocation_id">ID of the allocation</param>
    /// <param name="server_endpoints">Connection endpoints for the assigned relay server</param>
    /// <param name="relay_server">relay_server param</param>
    /// <param name="key">Base64-encoded key required for the HMAC signature of the BIND message</param>
    /// <param name="connection_data">Base64 encoded representation of an encrypted connection data blob describing this allocation. Required for establishing communication with other players.</param>
    /// <param name="allocation_id_bytes">Base64 encoded form of AllocationID. When decoded, this is the exact expected byte alignment to be used when crafting relay protocol messages that require AllocationID. eg. PING, CONNECT_REQUEST, RELAY, CLOSE, etc.</param>
    /// <param name="host_connection_data">Base64 encoded representation of an encrypted connection data blob describing the allocation and relay server of the player who created the join code. Used for establishing communication with the host.</param>
    [Preserve]
    [DataContract(Name = "JoinAllocation")]
    public class JoinAllocation
    {
        [Preserve]
        public JoinAllocation(System.Guid allocationId, List<RelayServerEndpoint> serverEndpoints, RelayServer relayServer, byte[] key, byte[] connectionData, byte[] allocationIdBytes, byte[] hostConnectionData)
        {
            AllocationId = allocationId;
            ServerEndpoints = serverEndpoints;
            RelayServer = relayServer;
            Key = key;
            ConnectionData = connectionData;
            AllocationIdBytes = allocationIdBytes;
            HostConnectionData = hostConnectionData;
        }

        [Preserve]
        [DataMember(Name = "allocation_id", IsRequired = true, EmitDefaultValue = true)]
        public System.Guid AllocationId{ get; }

        [Preserve]
        [DataMember(Name = "server_endpoints", IsRequired = true, EmitDefaultValue = true)]
        public List<RelayServerEndpoint> ServerEndpoints{ get; }

        [Preserve]
        [DataMember(Name = "relay_server", IsRequired = true, EmitDefaultValue = true)]
        public RelayServer RelayServer{ get; }

        [Preserve]
        [DataMember(Name = "key", IsRequired = true, EmitDefaultValue = true)]
        public byte[] Key{ get; }

        [Preserve]
        [DataMember(Name = "connection_data", IsRequired = true, EmitDefaultValue = true)]
        public byte[] ConnectionData{ get; }

        [Preserve]
        [DataMember(Name = "allocation_id_bytes", IsRequired = true, EmitDefaultValue = true)]
        public byte[] AllocationIdBytes{ get; }

        [Preserve]
        [DataMember(Name = "host_connection_data", IsRequired = true, EmitDefaultValue = true)]
        public byte[] HostConnectionData{ get; }
    
    }
}

