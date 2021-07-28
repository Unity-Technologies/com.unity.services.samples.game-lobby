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
    /// Additional detail about an error.  This may include detailed validation failure messages, debugging information, troubleshooting steps, or more.
    /// <param name="errorType">errorType param</param>
    /// <param name="message">message param</param>
    /// </summary>

    [Preserve]
    [DataContract(Name = "Detail")]
    public class Detail
    {
        /// <summary>
        /// Additional detail about an error.  This may include detailed validation failure messages, debugging information, troubleshooting steps, or more.
        /// </summary>
        /// <param name="errorType">errorType param</param>
        /// <param name="message">message param</param>
        [Preserve]
        public Detail(string errorType = default, string message = default)
        {
            ErrorType = errorType;
            Message = message;
        }

    
        [Preserve]
        [DataMember(Name = "errorType", EmitDefaultValue = false)]
        public string ErrorType{ get; }

        [Preserve]
        [DataMember(Name = "message", EmitDefaultValue = false)]
        public string Message{ get; }
    
    }
}

