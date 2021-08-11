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
    /// A request to create a join code
    /// <param name="allocationId">UUID of the allocation which will be mapped to the generated join code.  The connection data blob from the allocation will be shared with clients who use the generated join code on the /v1/join endpoint. This sharing is a mechanism to establish communication between players via the relay server data protocol</param>
    /// </summary>

    [Preserve]
    [DataContract(Name = "JoinCodeRequest")]
    public class JoinCodeRequest
    {
        /// <summary>
        /// A request to create a join code
        /// </summary>
        /// <param name="allocationId">UUID of the allocation which will be mapped to the generated join code.  The connection data blob from the allocation will be shared with clients who use the generated join code on the /v1/join endpoint. This sharing is a mechanism to establish communication between players via the relay server data protocol</param>
        [Preserve]
        public JoinCodeRequest(System.Guid allocationId)
        {
            AllocationId = allocationId;
        }

    
        /// <summary>
        /// UUID of the allocation which will be mapped to the generated join code.  The connection data blob from the allocation will be shared with clients who use the generated join code on the /v1/join endpoint. This sharing is a mechanism to establish communication between players via the relay server data protocol
        /// </summary>
        [Preserve]
        [DataMember(Name = "allocationId", IsRequired = true, EmitDefaultValue = true)]
        public System.Guid AllocationId{ get; }
    
    }
}

