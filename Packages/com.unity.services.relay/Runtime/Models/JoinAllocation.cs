using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Unity.Services.Relay.Http;



namespace Unity.Services.Relay.Models
{
    /// <summary>
    /// An allocation created via a join code
    /// <param name="allocationId">ID of the allocation</param>
    /// <param name="serverEndpoints">Connection endpoints for the assigned relay server</param>
    /// <param name="relayServer">relayServer param</param>
    /// <param name="key">Base64-encoded key required for the HMAC signature of the BIND message</param>
    /// <param name="connectionData">Base64 encoded representation of an encrypted connection data blob describing this allocation. Required for establishing communication with other players.</param>
    /// <param name="allocationIdBytes">Base64 encoded form of AllocationID. When decoded, this is the exact expected byte alignment to be used when crafting relay protocol messages that require AllocationID. eg. PING, CONNECT, RELAY, CLOSE, etc.</param>
    /// <param name="hostConnectionData">Base64 encoded representation of an encrypted connection data blob describing the allocation and relay server of the player who created the join code. Used for establishing communication with the host.</param>
    /// </summary>

    [Preserve]
    [DataContract(Name = "JoinAllocation")]
    public class JoinAllocation
    {
        /// <summary>
        /// An allocation created via a join code
        /// </summary>
        /// <param name="allocationId">ID of the allocation</param>
        /// <param name="serverEndpoints">Connection endpoints for the assigned relay server</param>
        /// <param name="relayServer">relayServer param</param>
        /// <param name="key">Base64-encoded key required for the HMAC signature of the BIND message</param>
        /// <param name="connectionData">Base64 encoded representation of an encrypted connection data blob describing this allocation. Required for establishing communication with other players.</param>
        /// <param name="allocationIdBytes">Base64 encoded form of AllocationID. When decoded, this is the exact expected byte alignment to be used when crafting relay protocol messages that require AllocationID. eg. PING, CONNECT, RELAY, CLOSE, etc.</param>
        /// <param name="hostConnectionData">Base64 encoded representation of an encrypted connection data blob describing the allocation and relay server of the player who created the join code. Used for establishing communication with the host.</param>
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

    
        /// <summary>
        /// ID of the allocation
        /// </summary>
        [Preserve]
        [DataMember(Name = "allocationId", IsRequired = true, EmitDefaultValue = true)]
        public System.Guid AllocationId{ get; }

        /// <summary>
        /// Connection endpoints for the assigned relay server
        /// </summary>
        [Preserve]
        [DataMember(Name = "serverEndpoints", IsRequired = true, EmitDefaultValue = true)]
        public List<RelayServerEndpoint> ServerEndpoints{ get; }

        [Preserve]
        [DataMember(Name = "relayServer", IsRequired = true, EmitDefaultValue = true)]
        public RelayServer RelayServer{ get; }

        /// <summary>
        /// Base64-encoded key required for the HMAC signature of the BIND message
        /// </summary>
        [Preserve]
        [DataMember(Name = "key", IsRequired = true, EmitDefaultValue = true)]
        public byte[] Key{ get; }

        /// <summary>
        /// Base64 encoded representation of an encrypted connection data blob describing this allocation. Required for establishing communication with other players.
        /// </summary>
        [Preserve]
        [DataMember(Name = "connectionData", IsRequired = true, EmitDefaultValue = true)]
        public byte[] ConnectionData{ get; }

        /// <summary>
        /// Base64 encoded form of AllocationID. When decoded, this is the exact expected byte alignment to be used when crafting relay protocol messages that require AllocationID. eg. PING, CONNECT, RELAY, CLOSE, etc.
        /// </summary>
        [Preserve]
        [DataMember(Name = "allocationIdBytes", IsRequired = true, EmitDefaultValue = true)]
        public byte[] AllocationIdBytes{ get; }

        /// <summary>
        /// Base64 encoded representation of an encrypted connection data blob describing the allocation and relay server of the player who created the join code. Used for establishing communication with the host.
        /// </summary>
        [Preserve]
        [DataMember(Name = "hostConnectionData", IsRequired = true, EmitDefaultValue = true)]
        public byte[] HostConnectionData{ get; }
    
    }
}

