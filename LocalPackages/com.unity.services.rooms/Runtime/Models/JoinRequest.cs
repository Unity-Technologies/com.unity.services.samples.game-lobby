using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace Unity.Services.Rooms.Models
{
    [Preserve]
    [DataContract(Name = "JoinRequest")]
    public class JoinRequest
    {
        [Preserve]
        public JoinRequest(string id = default(string), Player player = default(Player), string roomCode = default(string))
        {
            Id = id;
            Player = player;
            RoomCode = roomCode;
        }

        [Preserve]
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public string Id{ get; }

        [Preserve]
        [DataMember(Name = "player", EmitDefaultValue = false)]
        public Player Player{ get; }

        [Preserve]
        [DataMember(Name = "roomCode", EmitDefaultValue = false)]
        public string RoomCode{ get; }
    }
}
