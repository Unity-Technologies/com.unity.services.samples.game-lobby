using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;



namespace Unity.Services.Relay.Models
{
    /// <summary>
    /// ErrorResponseBody model
    /// </summary>
    /// <param name="status">MUST use the same status code in the actual HTTP response.</param>
    /// <param name="detail">A human-readable explanation specific to this occurrence of the problem. Ought to focus on helping the client correct the problem, rather than giving debugging information.</param>
    /// <param name="title">SHOULD be the same as the recommended HTTP status phrase for that code.</param>
    /// <param name="details">Machine readable list of individual errors.</param>
    [Preserve]
    [DataContract(Name = "ErrorResponseBody")]
    public class ErrorResponseBody
    {
        [Preserve]
        public ErrorResponseBody(int status, string detail, string title, List<KeyValuePair> details = default(List<KeyValuePair>))
        {
            Status = status;
            Detail = detail;
            Title = title;
            Details = details;
        }

        [Preserve]
        [DataMember(Name = "status", IsRequired = true, EmitDefaultValue = true)]
        public int Status{ get; }

        [Preserve]
        [DataMember(Name = "detail", IsRequired = true, EmitDefaultValue = true)]
        public string Detail{ get; }

        [Preserve]
        [DataMember(Name = "title", IsRequired = true, EmitDefaultValue = true)]
        public string Title{ get; }

        [Preserve]
        [DataMember(Name = "details", EmitDefaultValue = false)]
        public List<KeyValuePair> Details{ get; }
    
    }
}

