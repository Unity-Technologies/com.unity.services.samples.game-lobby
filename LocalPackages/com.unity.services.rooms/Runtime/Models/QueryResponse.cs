using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace Unity.Services.Rooms.Models
{
    [Preserve]
    [DataContract(Name = "QueryResponse")]
    public class QueryResponse
    {
        [Preserve]
        public QueryResponse(List<Room> results = default(List<Room>))
        {
            Results = results;
        }

        [Preserve]
        [DataMember(Name = "results", EmitDefaultValue = false)]
        public List<Room> Results{ get; }
    }
}
