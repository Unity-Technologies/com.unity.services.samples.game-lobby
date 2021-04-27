using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace Unity.Services.Rooms.Models
{
    [Preserve]
    [DataContract(Name = "PlayerUpdateRequest")]
    public class PlayerUpdateRequest
    {
        [Preserve]
        public PlayerUpdateRequest(string connectionInfo = default(string), Dictionary<string, DataObject> data = default(Dictionary<string, DataObject>))
        {
            ConnectionInfo = connectionInfo;
            Data = data;
        }

        [Preserve]
        [DataMember(Name = "connectionInfo", EmitDefaultValue = false)]
        public string ConnectionInfo{ get; }

        [Preserve]
        [DataMember(Name = "data", EmitDefaultValue = false)]
        public Dictionary<string, DataObject> Data{ get; }
    }
}
