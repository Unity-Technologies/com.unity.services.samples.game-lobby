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
    /// RegionsResponseBody model
    /// <param name="data">data param</param>
    /// </summary>

    [Preserve]
    [DataContract(Name = "RegionsResponseBody")]
    public class RegionsResponseBody
    {
        /// <summary>
        /// Creates an instance of RegionsResponseBody.
        /// </summary>
        /// <param name="data">data param</param>
        [Preserve]
        public RegionsResponseBody(RegionsData data)
        {
            Data = data;
        }

    
        [Preserve]
        [DataMember(Name = "data", IsRequired = true, EmitDefaultValue = true)]
        public RegionsData Data{ get; }
    
    }
}

