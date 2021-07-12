using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;



namespace Unity.Services.Lobbies.Models
{
    /// <summary>
    /// The body that will be returned for any failing request.  We are using the [RFC 7807 Error Format](https://www.rfc-editor.org/rfc/rfc7807.html#section-3.1).
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
        public ErrorStatus(int status = default(int), string title = default(string), List<Detail> details = default(List<Detail>))
        {
            Status = status;
            Title = title;
            Details = details;
        }

        /// <summary>
        /// status param
        /// </summary>
        [Preserve]
        [DataMember(Name = "status", EmitDefaultValue = false)]
        public int Status{ get; }

        /// <summary>
        /// title param
        /// </summary>
        [Preserve]
        [DataMember(Name = "title", EmitDefaultValue = false)]
        public string Title{ get; }

        /// <summary>
        /// details param
        /// </summary>
        [Preserve]
        [DataMember(Name = "details", EmitDefaultValue = false)]
        public List<Detail> Details{ get; }
    
    }
}

