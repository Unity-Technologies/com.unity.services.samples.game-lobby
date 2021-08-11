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
    /// Metadata for a response returned from an API call
    /// <param name="requestId">Unique ID for this request that triggered this response</param>
    /// <param name="status">Indicates the HTTP status code of the response</param>
    /// </summary>

    [Preserve]
    [DataContract(Name = "ResponseMeta")]
    public class ResponseMeta
    {
        /// <summary>
        /// Metadata for a response returned from an API call
        /// </summary>
        /// <param name="requestId">Unique ID for this request that triggered this response</param>
        /// <param name="status">Indicates the HTTP status code of the response</param>
        [Preserve]
        public ResponseMeta(string requestId, int status)
        {
            RequestId = requestId;
            Status = status;
        }

    
        /// <summary>
        /// Unique ID for this request that triggered this response
        /// </summary>
        [Preserve]
        [DataMember(Name = "requestId", IsRequired = true, EmitDefaultValue = true)]
        public string RequestId{ get; }

        /// <summary>
        /// Indicates the HTTP status code of the response
        /// </summary>
        [Preserve]
        [DataMember(Name = "status", IsRequired = true, EmitDefaultValue = true)]
        public int Status{ get; }
    
    }
}

