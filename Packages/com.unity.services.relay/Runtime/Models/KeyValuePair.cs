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
    /// KeyValuePair model
    /// <param name="key">key param</param>
    /// <param name="value">value param</param>
    /// </summary>

    [Preserve]
    [DataContract(Name = "KeyValuePair")]
    public class KeyValuePair
    {
        /// <summary>
        /// Creates an instance of KeyValuePair.
        /// </summary>
        /// <param name="key">key param</param>
        /// <param name="value">value param</param>
        [Preserve]
        public KeyValuePair(string key, string value)
        {
            Key = key;
            Value = value;
        }

    
        [Preserve]
        [DataMember(Name = "key", IsRequired = true, EmitDefaultValue = true)]
        public string Key{ get; }

        [Preserve]
        [DataMember(Name = "value", IsRequired = true, EmitDefaultValue = true)]
        public string Value{ get; }
    
    }
}

