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
    /// ErrorResponseBody model
    /// <param name="status">MUST use the same status code in the actual HTTP response.</param>
    /// <param name="detail">A human-readable explanation specific to this occurrence of the problem. Ought to focus on helping the client correct the problem, rather than giving debugging information.</param>
    /// <param name="title">SHOULD be the same as the recommended HTTP status phrase for that code.</param>
    /// <param name="details">Machine readable list of individual errors.</param>
    /// </summary>

    [Preserve]
    [DataContract(Name = "ErrorResponseBody")]
    public class ErrorResponseBody
    {
        /// <summary>
        /// Creates an instance of ErrorResponseBody.
        /// </summary>
        /// <param name="status">MUST use the same status code in the actual HTTP response.</param>
        /// <param name="detail">A human-readable explanation specific to this occurrence of the problem. Ought to focus on helping the client correct the problem, rather than giving debugging information.</param>
        /// <param name="title">SHOULD be the same as the recommended HTTP status phrase for that code.</param>
        /// <param name="details">Machine readable list of individual errors.</param>
        [Preserve]
        public ErrorResponseBody(int status, string detail, string title, List<KeyValuePair> details = default)
        {
            Status = status;
            Detail = detail;
            Title = title;
            Details = details;
        }

    
        /// <summary>
        /// MUST use the same status code in the actual HTTP response.
        /// </summary>
        [Preserve]
        [DataMember(Name = "status", IsRequired = true, EmitDefaultValue = true)]
        public int Status{ get; }

        /// <summary>
        /// A human-readable explanation specific to this occurrence of the problem. Ought to focus on helping the client correct the problem, rather than giving debugging information.
        /// </summary>
        [Preserve]
        [DataMember(Name = "detail", IsRequired = true, EmitDefaultValue = true)]
        public string Detail{ get; }

        /// <summary>
        /// SHOULD be the same as the recommended HTTP status phrase for that code.
        /// </summary>
        [Preserve]
        [DataMember(Name = "title", IsRequired = true, EmitDefaultValue = true)]
        public string Title{ get; }

        /// <summary>
        /// Machine readable list of individual errors.
        /// </summary>
        [Preserve]
        [DataMember(Name = "details", EmitDefaultValue = false)]
        public List<KeyValuePair> Details{ get; }
    
    }
}

