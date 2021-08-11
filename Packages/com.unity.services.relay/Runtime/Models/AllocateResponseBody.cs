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
    /// AllocateResponseBody model
    /// <param name="meta">meta param</param>
    /// <param name="links">links param</param>
    /// <param name="data">data param</param>
    /// </summary>

    [Preserve]
    [DataContract(Name = "AllocateResponseBody")]
    public class AllocateResponseBody
    {
        /// <summary>
        /// Creates an instance of AllocateResponseBody.
        /// </summary>
        /// <param name="meta">meta param</param>
        /// <param name="data">data param</param>
        /// <param name="links">links param</param>
        [Preserve]
        public AllocateResponseBody(ResponseMeta meta, AllocationData data, ResponseLinks links = default)
        {
            Meta = meta;
            Links = links;
            Data = data;
        }

    
        [Preserve]
        [DataMember(Name = "meta", IsRequired = true, EmitDefaultValue = true)]
        public ResponseMeta Meta{ get; }

        [Preserve]
        [DataMember(Name = "links", EmitDefaultValue = false)]
        public ResponseLinks Links{ get; }

        [Preserve]
        [DataMember(Name = "data", IsRequired = true, EmitDefaultValue = true)]
        public AllocationData Data{ get; }
    
    }
}

