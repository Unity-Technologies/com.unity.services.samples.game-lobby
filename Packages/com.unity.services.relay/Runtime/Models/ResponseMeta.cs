using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;



namespace Unity.Services.Relay.Models
{
    /// <summary>
    /// Metadata for a response returned from an API call
    /// </summary>
    /// <param name="request_id">Unique ID for this request that triggered this response</param>
    /// <param name="status">Indicates the HTTP status code of the response</param>
    [Preserve]
    [DataContract(Name = "ResponseMeta")]
    public class ResponseMeta
    {
        [Preserve]
        public ResponseMeta(string requestId, int status)
        {
            RequestId = requestId;
            Status = status;
        }

        [Preserve]
        [DataMember(Name = "request_id", IsRequired = true, EmitDefaultValue = true)]
        public string RequestId{ get; }

        [Preserve]
        [DataMember(Name = "status", IsRequired = true, EmitDefaultValue = true)]
        public int Status{ get; }
    
    }
}

