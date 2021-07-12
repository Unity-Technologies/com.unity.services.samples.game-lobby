using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;



namespace Unity.Services.Lobbies.Models
{
    /// <summary>
    /// Data about an individual lobby.
    /// </summary>
    [Preserve]
    [DataContract(Name = "Lobby")]
    public class Lobby
    {
        /// <summary>
        /// Data about an individual lobby.
        /// </summary>
        /// <param name="id">id param</param>
        /// <param name="lobbyCode">A short code that be used to join a lobby. This is only visible to lobby members. Typically this is displayed to the user so they can share it with other players out of game. Users with the code can join the lobby even when it is private.</param>
        /// <param name="upid">The Unity project ID of the game.</param>
        /// <param name="name">The name of the lobby. Typically this shown in game UI to represent the lobby.</param>
        /// <param name="maxPlayers">The maximum number of players that can be members of the lobby.</param>
        /// <param name="availableSlots">The number of remaining open slots for players before the lobby becomes full.</param>
        /// <param name="isPrivate">Whether the lobby is private or not. Private lobbies do not appear in query results.</param>
        /// <param name="players">The members of the lobby.</param>
        /// <param name="data">Properties of the lobby set by the host.</param>
        /// <param name="hostId">The ID of the player that is the lobby host.</param>
        /// <param name="created">When the lobby was created. The timestamp is in UTC and conforms to ISO 8601.</param>
        /// <param name="lastUpdated">When the lobby was last updated. The timestamp is in UTC and conforms to ISO 8601.</param>
        [Preserve]
        public Lobby(string id = default(string), string lobbyCode = null, string upid = default(string), string name = null, int maxPlayers = default(int), int availableSlots = default(int), bool isPrivate = default(bool), List<Player> players = default(List<Player>), Dictionary<string, DataObject> data = null, string hostId = default(string), DateTime created = default(DateTime), DateTime lastUpdated = default(DateTime))
        {
            Id = id;
            LobbyCode = lobbyCode;
            Upid = upid;
            Name = name;
            MaxPlayers = maxPlayers;
            AvailableSlots = availableSlots;
            IsPrivate = isPrivate;
            Players = players;
            Data = data;
            HostId = hostId;
            Created = created;
            LastUpdated = lastUpdated;
        }

        /// <summary>
        /// id param
        /// </summary>
        [Preserve]
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public string Id{ get; }

        /// <summary>
        /// A short code that be used to join a lobby. This is only visible to lobby members. Typically this is displayed to the user so they can share it with other players out of game. Users with the code can join the lobby even when it is private.
        /// </summary>
        [Preserve]
        [DataMember(Name = "lobbyCode", EmitDefaultValue = false)]
        public string LobbyCode{ get; }

        /// <summary>
        /// The Unity project ID of the game.
        /// </summary>
        [Preserve]
        [DataMember(Name = "upid", EmitDefaultValue = false)]
        public string Upid{ get; }

        /// <summary>
        /// The name of the lobby. Typically this shown in game UI to represent the lobby.
        /// </summary>
        [Preserve]
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name{ get; }

        /// <summary>
        /// The maximum number of players that can be members of the lobby.
        /// </summary>
        [Preserve]
        [DataMember(Name = "maxPlayers", EmitDefaultValue = false)]
        public int MaxPlayers{ get; }

        /// <summary>
        /// The number of remaining open slots for players before the lobby becomes full.
        /// </summary>
        [Preserve]
        [DataMember(Name = "availableSlots", EmitDefaultValue = false)]
        public int AvailableSlots{ get; }

        /// <summary>
        /// Whether the lobby is private or not. Private lobbies do not appear in query results.
        /// </summary>
        [Preserve]
        [DataMember(Name = "isPrivate", EmitDefaultValue = true)]
        public bool IsPrivate{ get; }

        /// <summary>
        /// The members of the lobby.
        /// </summary>
        [Preserve]
        [DataMember(Name = "players", EmitDefaultValue = false)]
        public List<Player> Players{ get; }

        /// <summary>
        /// Properties of the lobby set by the host.
        /// </summary>
        [Preserve]
        [DataMember(Name = "data", EmitDefaultValue = false)]
        public Dictionary<string, DataObject> Data{ get; }

        /// <summary>
        /// The ID of the player that is the lobby host.
        /// </summary>
        [Preserve]
        [DataMember(Name = "hostId", EmitDefaultValue = false)]
        public string HostId{ get; }

        /// <summary>
        /// When the lobby was created. The timestamp is in UTC and conforms to ISO 8601.
        /// </summary>
        [Preserve]
        [DataMember(Name = "created", EmitDefaultValue = false)]
        public DateTime Created{ get; }

        /// <summary>
        /// When the lobby was last updated. The timestamp is in UTC and conforms to ISO 8601.
        /// </summary>
        [Preserve]
        [DataMember(Name = "lastUpdated", EmitDefaultValue = false)]
        public DateTime LastUpdated{ get; }
    
    }
}

