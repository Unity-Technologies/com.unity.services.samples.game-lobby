using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace Unity.Services.Rooms.Models
{
    [Preserve]
    [DataContract(Name = "ErrorStatus")]
    public class ErrorStatus
    {
        [Preserve]
        public ErrorStatus(List<Detail> details = default(List<Detail>), int status = default(int), string title = default(string))
        {
            Details = details;
            Status = status;
            Title = title;
        }

        [Preserve]
        [DataMember(Name = "details", EmitDefaultValue = false)]
        public List<Detail> Details{ get; }

        [Preserve]
        [DataMember(Name = "status", EmitDefaultValue = false)]
        public int Status{ get; }

        [Preserve]
        [DataMember(Name = "title", EmitDefaultValue = false)]
        public string Title{ get; }
    }
}
