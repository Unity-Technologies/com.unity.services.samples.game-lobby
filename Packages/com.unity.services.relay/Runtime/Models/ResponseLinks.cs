using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;



namespace Unity.Services.Relay.Models
{
    /// <summary>
    /// Represents links to related API endpoints
    /// </summary>
    /// <param name="next">Indicates the URL of the suggest API endpoint to call next</param>
    [Preserve]
    [DataContract(Name = "ResponseLinks")]
    public class ResponseLinks
    {
        [Preserve]
        public ResponseLinks(string next = default(string))
        {
            Next = next;
        }

        [Preserve]
        [DataMember(Name = "next", EmitDefaultValue = false)]
        public string Next{ get; }
    
    }
}

