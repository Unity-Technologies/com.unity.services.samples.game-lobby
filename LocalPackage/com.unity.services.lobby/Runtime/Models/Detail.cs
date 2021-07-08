using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;



namespace Unity.Services.Lobbies.Models
{
    /// <summary>
    /// Additional detail about an error.  This may include detailed validation failure messages, debugging information, troubleshooting steps, or more.
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
        public Detail(string errorType = default(string), string message = default(string))
        {
            ErrorType = errorType;
            Message = message;
        }

        /// <summary>
        /// errorType param
        /// </summary>
        [Preserve]
        [DataMember(Name = "errorType", EmitDefaultValue = false)]
        public string ErrorType{ get; }

        /// <summary>
        /// message param
        /// </summary>
        [Preserve]
        [DataMember(Name = "message", EmitDefaultValue = false)]
        public string Message{ get; }
    
    }
}

