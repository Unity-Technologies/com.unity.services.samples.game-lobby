using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;



namespace Unity.Services.Relay.Models
{
    /// <summary>
    /// A request to create an allocation
    /// </summary>
    /// <param name="max_connections">Indicates the maximum number of connections that the client will allow to communicate with them.  It will also be used in order to find a relay with sufficient capacity</param>
    [Preserve]
    [DataContract(Name = "AllocationRequest")]
    public class AllocationRequest
    {
        [Preserve]
        public AllocationRequest(int maxConnections)
        {
            MaxConnections = maxConnections;
        }

        [Preserve]
        [DataMember(Name = "max_connections", IsRequired = true, EmitDefaultValue = true)]
        public int MaxConnections{ get; }
    
    }
}

