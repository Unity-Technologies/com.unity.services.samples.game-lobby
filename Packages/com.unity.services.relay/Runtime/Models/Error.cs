using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace Unity.Services.Relay.Models
{
    /// <summary>
    /// Error model
    /// </summary>
    /// <param name="title">A short, human-readable summary of the problem that SHOULD NOT change from occurrence to occurrence of the problem, except for purposes of localization.</param>
    /// <param name="detail">A human-readable explanation specific to this occurrence of the problem</param>
    [Preserve]
    [DataContract(Name = "Error")]
    public class Error
    {
        [Preserve]
        public Error(string title, string detail)
        {
            Title = title;
            Detail = detail;
        }

        [Preserve]
        [DataMember(Name = "title", IsRequired = true, EmitDefaultValue = true)]
        public string Title{ get; }

        [Preserve]
        [DataMember(Name = "detail", IsRequired = true, EmitDefaultValue = true)]
        public string Detail{ get; }
    }
}
