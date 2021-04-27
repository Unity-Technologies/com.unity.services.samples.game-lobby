using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace Unity.Services.Rooms.Models
{
    [Preserve]
    [DataContract(Name = "DataObject")]
    public class DataObject
    {
        [Preserve]
        public DataObject(string index = default(string), string value = default(string), string visibility = default(string))
        {
            Index = index;
            Value = value;
            Visibility = visibility;
        }

        [Preserve]
        [DataMember(Name = "index", EmitDefaultValue = false)]
        public string Index{ get; }

        [Preserve]
        [DataMember(Name = "value", EmitDefaultValue = false)]
        public string Value{ get; }

        [Preserve]
        [DataMember(Name = "visibility", EmitDefaultValue = false)]
        public string Visibility{ get; }
    }
}
