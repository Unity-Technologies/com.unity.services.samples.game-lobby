using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace Unity.Services.Rooms.Models
{
    [Preserve]
    [DataContract(Name = "QueryRequest")]
    public class QueryRequest
    {
        [Preserve]
        public QueryRequest(int? count = default(int?), List<QueryFilter> filter = default(List<QueryFilter>))
        {
            Count = count;
            Filter = filter;
        }

        [Preserve]
        [DataMember(Name = "count", EmitDefaultValue = false)]
        public int? Count{ get; }

        [Preserve]
        [DataMember(Name = "filter", EmitDefaultValue = false)]
        public List<QueryFilter> Filter{ get; }
    }
}
