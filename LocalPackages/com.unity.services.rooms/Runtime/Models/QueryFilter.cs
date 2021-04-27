using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace Unity.Services.Rooms.Models
{
    [Preserve]
    [DataContract(Name = "QueryFilter")]
    public class QueryFilter
    {
        [Preserve]
        public QueryFilter(string field = default(string), string op = default(string), string value = default(string))
        {
            Field = field;
            Op = op;
            Value = value;
        }

        [Preserve]
        [DataMember(Name = "field", EmitDefaultValue = false)]
        public string Field{ get; }

        [Preserve]
        [DataMember(Name = "op", EmitDefaultValue = false)]
        public string Op{ get; }

        [Preserve]
        [DataMember(Name = "value", EmitDefaultValue = false)]
        public string Value{ get; }
    }
}
