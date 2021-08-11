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
    /// JoinResponseBody model
    /// <param name="meta">meta param</param>
    /// <param name="data">data param</param>
    /// </summary>

    [Preserve]
    [DataContract(Name = "JoinResponseBody")]
    public class JoinResponseBody
    {
        /// <summary>
        /// Creates an instance of JoinResponseBody.
        /// </summary>
        /// <param name="meta">meta param</param>
        /// <param name="data">data param</param>
        [Preserve]
        public JoinResponseBody(ResponseMeta meta, JoinData data)
        {
            Meta = meta;
            Data = data;
        }

    
        [Preserve]
        [DataMember(Name = "meta", IsRequired = true, EmitDefaultValue = true)]
        public ResponseMeta Meta{ get; }

        [Preserve]
        [DataMember(Name = "data", IsRequired = true, EmitDefaultValue = true)]
        public JoinData Data{ get; }
    
    }
}

