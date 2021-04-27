using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace Unity.Services.Rooms.Models
{
    [Preserve]
    [DataContract(Name = "UpdateRequest")]
    public class UpdateRequest
    {
        [Preserve]
        public UpdateRequest(Dictionary<string, DataObject> data = default(Dictionary<string, DataObject>), string hostId = default(string), bool? isPrivate = default(bool?), int? maxPlayers = default(int?), string name = default(string))
        {
            Data = data;
            HostId = hostId;
            IsPrivate = isPrivate;
            MaxPlayers = maxPlayers;
            Name = name;
        }

        [Preserve]
        [DataMember(Name = "data", EmitDefaultValue = false)]
        public Dictionary<string, DataObject> Data{ get; }

        [Preserve]
        [DataMember(Name = "hostId", EmitDefaultValue = false)]
        public string HostId{ get; }

        [Preserve]
        [DataMember(Name = "isPrivate", EmitDefaultValue = true)]
        public bool? IsPrivate{ get; }

        [Preserve]
        [DataMember(Name = "maxPlayers", EmitDefaultValue = false)]
        public int? MaxPlayers{ get; }

        [Preserve]
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name{ get; }
    }
}
