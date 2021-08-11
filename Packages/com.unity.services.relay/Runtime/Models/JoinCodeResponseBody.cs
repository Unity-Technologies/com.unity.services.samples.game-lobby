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
    /// JoinCodeResponseBody model
    /// <param name="meta">meta param</param>
    /// <param name="links">links param</param>
    /// <param name="data">data param</param>
    /// </summary>

    [Preserve]
    [DataContract(Name = "JoinCodeResponseBody")]
    public class JoinCodeResponseBody
    {
        /// <summary>
        /// Creates an instance of JoinCodeResponseBody.
        /// </summary>
        /// <param name="meta">meta param</param>
        /// <param name="links">links param</param>
        /// <param name="data">data param</param>
        [Preserve]
        public JoinCodeResponseBody(ResponseMeta meta, ResponseLinks links, JoinCodeData data)
        {
            Meta = meta;
            Links = links;
            Data = data;
        }

    
        [Preserve]
        [DataMember(Name = "meta", IsRequired = true, EmitDefaultValue = true)]
        public ResponseMeta Meta{ get; }

        [Preserve]
        [DataMember(Name = "links", IsRequired = true, EmitDefaultValue = true)]
        public ResponseLinks Links{ get; }

        [Preserve]
        [DataMember(Name = "data", IsRequired = true, EmitDefaultValue = true)]
        public JoinCodeData Data{ get; }
    
    }
}

