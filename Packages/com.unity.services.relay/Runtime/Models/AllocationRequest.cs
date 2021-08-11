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
    /// A request to create an allocation
    /// <param name="maxConnections">Indicates the maximum number of connections that the client will allow to communicate with them.  It will also be used in order to find a relay with sufficient capacity</param>
    /// <param name="region">Indicates the region this allocation should go. If not provided, a default region will be chosen.</param>
    /// </summary>

    [Preserve]
    [DataContract(Name = "AllocationRequest")]
    public class AllocationRequest
    {
        /// <summary>
        /// A request to create an allocation
        /// </summary>
        /// <param name="maxConnections">Indicates the maximum number of connections that the client will allow to communicate with them.  It will also be used in order to find a relay with sufficient capacity</param>
        /// <param name="region">Indicates the region this allocation should go. If not provided, a default region will be chosen.</param>
        [Preserve]
        public AllocationRequest(int maxConnections, string region = default)
        {
            MaxConnections = maxConnections;
            Region = region;
        }

    
        /// <summary>
        /// Indicates the maximum number of connections that the client will allow to communicate with them.  It will also be used in order to find a relay with sufficient capacity
        /// </summary>
        [Preserve]
        [DataMember(Name = "maxConnections", IsRequired = true, EmitDefaultValue = true)]
        public int MaxConnections{ get; }

        /// <summary>
        /// Indicates the region this allocation should go. If not provided, a default region will be chosen.
        /// </summary>
        [Preserve]
        [DataMember(Name = "region", EmitDefaultValue = false)]
        public string Region{ get; }
    
    }
}

