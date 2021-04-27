using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace Unity.Services.Rooms.Models
{
    [Preserve]
    [DataContract(Name = "CreateRequest")]
    public class CreateRequest
    {
        [Preserve]
        public CreateRequest(string name, Player player, Dictionary<string, DataObject> data = default(Dictionary<string, DataObject>), bool? isPrivate = false, int? maxPlayers = default(int?))
        {
            Name = name;
            Player = player;
            Data = data;
            IsPrivate = isPrivate;
            MaxPlayers = maxPlayers;
        }

        [Preserve]
        [DataMember(Name = "name", IsRequired = true, EmitDefaultValue = false)]
        public string Name{ get; }

        [Preserve]
        [DataMember(Name = "player", IsRequired = true, EmitDefaultValue = false)]
        public Player Player{ get; }

        [Preserve]
        [DataMember(Name = "data", EmitDefaultValue = false)]
        public Dictionary<string, DataObject> Data{ get; }

        [Preserve]
        [DataMember(Name = "isPrivate", EmitDefaultValue = true)]
        public bool? IsPrivate{ get; }

        [Preserve]
        [DataMember(Name = "maxPlayers", EmitDefaultValue = false)]
        public int? MaxPlayers{ get; }
    }
}
