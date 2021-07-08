using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;



namespace Unity.Services.Relay.Models
{
    /// <summary>
    /// AllocateResponseBody model
    /// </summary>
    /// <param name="meta">meta param</param>
    /// <param name="data">data param</param>
    /// <param name="links">links param</param>
    [Preserve]
    [DataContract(Name = "AllocateResponseBody")]
    public class AllocateResponseBody
    {
        [Preserve]
        public AllocateResponseBody(ResponseMeta meta, AllocationData data, ResponseLinks links = default(ResponseLinks))
        {
            Meta = meta;
            Data = data;
            Links = links;
        }

        [Preserve]
        [DataMember(Name = "meta", IsRequired = true, EmitDefaultValue = true)]
        public ResponseMeta Meta{ get; }

        [Preserve]
        [DataMember(Name = "data", IsRequired = true, EmitDefaultValue = true)]
        public AllocationData Data{ get; }

        [Preserve]
        [DataMember(Name = "links", EmitDefaultValue = false)]
        public ResponseLinks Links{ get; }
    
    }
}

