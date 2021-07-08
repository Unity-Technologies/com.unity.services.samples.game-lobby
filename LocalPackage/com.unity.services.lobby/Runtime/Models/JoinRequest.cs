using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;



namespace Unity.Services.Lobbies.Models
{
    /// <summary>
    /// The body of a Join Lobby request.
    /// </summary>
    [Preserve]
    [DataContract(Name = "JoinRequest")]
    public class JoinRequest
    {
        /// <summary>
        /// The body of a Join Lobby request.
        /// </summary>
        /// <param name="id">The id of the lobby to join.  Mutually exclusive with &#x60;lobbyCode&#x60;.  This is used to join a public lobby that has been discovered via a Query request.</param>
        /// <param name="lobbyCode">The lobby code of the lobby the join.  Mutually exclusive with &#x60;id&#x60;.  This is used to join a private lobby where the lobby code was shared to other users manually.</param>
        /// <param name="player">player param</param>
        [Preserve]
        public JoinRequest(string id = null, string lobbyCode = null, Player player = default(Player))
        {
            Id = id;
            LobbyCode = lobbyCode;
            Player = player;
        }

        /// <summary>
        /// The id of the lobby to join.  Mutually exclusive with &#x60;lobbyCode&#x60;.  This is used to join a public lobby that has been discovered via a Query request.
        /// </summary>
        [Preserve]
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public string Id{ get; }

        /// <summary>
        /// The lobby code of the lobby the join.  Mutually exclusive with &#x60;id&#x60;.  This is used to join a private lobby where the lobby code was shared to other users manually.
        /// </summary>
        [Preserve]
        [DataMember(Name = "lobbyCode", EmitDefaultValue = false)]
        public string LobbyCode{ get; }

        /// <summary>
        /// player param
        /// </summary>
        [Preserve]
        [DataMember(Name = "player", EmitDefaultValue = false)]
        public Player Player{ get; }
    
    }
}

