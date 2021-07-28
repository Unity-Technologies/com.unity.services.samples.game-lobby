using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Unity.Services.Lobbies.Http;



namespace Unity.Services.Lobbies.Models
{
    /// <summary>
    /// The body that will be returned for any failing request.  We are using the [RFC 7807 Error Format](https://www.rfc-editor.org/rfc/rfc7807.html#section-3.1).
    /// <param name="status">status param</param>
    /// <param name="title">title param</param>
    /// <param name="details">details param</param>
    /// </summary>

    [Preserve]
    [DataContract(Name = "ErrorStatus")]
    public class ErrorStatus
    {
        /// <summary>
        /// The body that will be returned for any failing request.  We are using the [RFC 7807 Error Format](https://www.rfc-editor.org/rfc/rfc7807.html#section-3.1).
        /// </summary>
        /// <param name="status">status param</param>
        /// <param name="title">title param</param>
        /// <param name="details">details param</param>
        [Preserve]
        public ErrorStatus(int status = default, string title = default, List<Detail> details = default)
        {
            Status = status;
            Title = title;
            Details = details;
        }

    
        [Preserve]
        [DataMember(Name = "status", EmitDefaultValue = false)]
        public int Status{ get; }

        [Preserve]
        [DataMember(Name = "title", EmitDefaultValue = false)]
        public string Title{ get; }

        [Preserve]
        [DataMember(Name = "details", EmitDefaultValue = false)]
        public List<Detail> Details{ get; }
    
    }
}

