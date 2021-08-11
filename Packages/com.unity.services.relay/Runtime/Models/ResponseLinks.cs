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
    /// Represents links to related API endpoints
    /// <param name="next">Indicates the URL of the suggest API endpoint to call next</param>
    /// </summary>

    [Preserve]
    [DataContract(Name = "ResponseLinks")]
    public class ResponseLinks
    {
        /// <summary>
        /// Represents links to related API endpoints
        /// </summary>
        /// <param name="next">Indicates the URL of the suggest API endpoint to call next</param>
        [Preserve]
        public ResponseLinks(string next = default)
        {
            Next = next;
        }

    
        /// <summary>
        /// Indicates the URL of the suggest API endpoint to call next
        /// </summary>
        [Preserve]
        [DataMember(Name = "next", EmitDefaultValue = false)]
        public string Next{ get; }
    
    }
}

