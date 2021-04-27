using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace Unity.Services.Rooms.Models
{
    [Preserve]
    [DataContract(Name = "Room")]
    public class Room
    {
        [Preserve]
        public Room(int currentPlayers = default(int), Dictionary<string, DataObject> data = default(Dictionary<string, DataObject>), string hostId = default(string), string id = default(string), bool isPrivate = default(bool), int maxPlayers = default(int), string name = default(string), List<Player> players = default(List<Player>), string roomCode = default(string), string upid = default(string))
        {
            CurrentPlayers = currentPlayers;
            Data = data;
            HostId = hostId;
            Id = id;
            IsPrivate = isPrivate;
            MaxPlayers = maxPlayers;
            Name = name;
            Players = players;
            RoomCode = roomCode;
            Upid = upid;
        }

        [Preserve]
        [DataMember(Name = "currentPlayers", EmitDefaultValue = false)]
        public int CurrentPlayers{ get; }

        [Preserve]
        [DataMember(Name = "data", EmitDefaultValue = false)]
        public Dictionary<string, DataObject> Data{ get; }

        [Preserve]
        [DataMember(Name = "hostId", EmitDefaultValue = false)]
        public string HostId{ get; }

        [Preserve]
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public string Id{ get; }

        [Preserve]
        [DataMember(Name = "isPrivate", EmitDefaultValue = true)]
        public bool IsPrivate{ get; }

        [Preserve]
        [DataMember(Name = "maxPlayers", EmitDefaultValue = false)]
        public int MaxPlayers{ get; }

        [Preserve]
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name{ get; }

        [Preserve]
        [DataMember(Name = "players", EmitDefaultValue = false)]
        public List<Player> Players{ get; }

        [Preserve]
        [DataMember(Name = "roomCode", EmitDefaultValue = false)]
        public string RoomCode{ get; }

        [Preserve]
        [DataMember(Name = "upid", EmitDefaultValue = false)]
        public string Upid{ get; }
    }
}
