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
    /// Details of an allocation to a relay server
    /// <param name="allocationId">ID of the allocation</param>
    /// <param name="serverEndpoints">Connection endpoints for the assigned relay server</param>
    /// <param name="relayServer">relayServer param</param>
    /// <param name="key">Base64-encoded key required for the HMAC signature of the BIND message</param>
    /// <param name="connectionData">Base64 encoded representation of an encrypted connection data blob describing this allocation. Required for establishing communication with other players.</param>
    /// <param name="allocationIdBytes">Base64 encoded form of AllocationID. When decoded, this is the exact expected byte alignment to be used when crafting relay protocol messages that require AllocationID. eg. PING, CONNECT, RELAY, CLOSE, etc.</param>
    /// </summary>

    [Preserve]
    [DataContract(Name = "Allocation")]
    public class Allocation
    {
        /// <summary>
        /// Details of an allocation to a relay server
        /// </summary>
        /// <param name="allocationId">ID of the allocation</param>
        /// <param name="serverEndpoints">Connection endpoints for the assigned relay server</param>
        /// <param name="relayServer">relayServer param</param>
        /// <param name="key">Base64-encoded key required for the HMAC signature of the BIND message</param>
        /// <param name="connectionData">Base64 encoded representation of an encrypted connection data blob describing this allocation. Required for establishing communication with other players.</param>
        /// <param name="allocationIdBytes">Base64 encoded form of AllocationID. When decoded, this is the exact expected byte alignment to be used when crafting relay protocol messages that require AllocationID. eg. PING, CONNECT, RELAY, CLOSE, etc.</param>
        [Preserve]
        public Allocation(System.Guid allocationId, List<RelayServerEndpoint> serverEndpoints, RelayServer relayServer, byte[] key, byte[] connectionData, byte[] allocationIdBytes)
        {
            AllocationId = allocationId;
            ServerEndpoints = serverEndpoints;
            RelayServer = relayServer;
            Key = key;
            ConnectionData = connectionData;
            AllocationIdBytes = allocationIdBytes;
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
    
    }
}

