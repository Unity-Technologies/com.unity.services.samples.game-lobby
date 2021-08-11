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
    /// JoinAllocationAllOf model
    /// <param name="hostConnectionData">Base64 encoded representation of an encrypted connection data blob describing the allocation and relay server of the player who created the join code. Used for establishing communication with the host.</param>
    /// </summary>

    [Preserve]
    [DataContract(Name = "JoinAllocation_allOf")]
    public class JoinAllocationAllOf
    {
        /// <summary>
        /// Creates an instance of JoinAllocationAllOf.
        /// </summary>
        /// <param name="hostConnectionData">Base64 encoded representation of an encrypted connection data blob describing the allocation and relay server of the player who created the join code. Used for establishing communication with the host.</param>
        [Preserve]
        public JoinAllocationAllOf(byte[] hostConnectionData)
        {
            HostConnectionData = hostConnectionData;
        }

    
        /// <summary>
        /// Base64 encoded representation of an encrypted connection data blob describing the allocation and relay server of the player who created the join code. Used for establishing communication with the host.
        /// </summary>
        [Preserve]
        [DataMember(Name = "hostConnectionData", IsRequired = true, EmitDefaultValue = true)]
        public byte[] HostConnectionData{ get; }
    
    }
}

