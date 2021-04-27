using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace Unity.Services.Rooms.Models
{
    [Preserve]
    [DataContract(Name = "Detail")]
    public class Detail
    {
        [Preserve]
        public Detail(string errorType = default(string), string message = default(string))
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
