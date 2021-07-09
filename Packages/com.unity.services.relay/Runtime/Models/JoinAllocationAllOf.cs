using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;



namespace Unity.Services.Relay.Models
{
    /// <summary>
    /// JoinAllocationAllOf model
    /// </summary>
    /// <param name="host_connection_data">Base64 encoded representation of an encrypted connection data blob describing the allocation and relay server of the player who created the join code. Used for establishing communication with the host.</param>
    [Preserve]
    [DataContract(Name = "JoinAllocation_allOf")]
    public class JoinAllocationAllOf
    {
        [Preserve]
        public JoinAllocationAllOf(byte[] hostConnectionData)
        {
            HostConnectionData = hostConnectionData;
        }

        [Preserve]
        [DataMember(Name = "host_connection_data", IsRequired = true, EmitDefaultValue = true)]
        public byte[] HostConnectionData{ get; }
    
    }
}

