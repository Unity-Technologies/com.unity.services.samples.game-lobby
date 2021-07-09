using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;



namespace Unity.Services.Lobbies.Models
{
    /// <summary>
    /// The body of a Join Lobby request using lobby code.
    /// </summary>
    [Preserve]
    [DataContract(Name = "JoinByCodeRequest")]
    public class JoinByCodeRequest
    {
        /// <summary>
        /// The body of a Join Lobby request using lobby code.
        /// </summary>
        /// <param name="lobbyCode">The lobby code of the lobby the join.  Mutually exclusive with &#x60;id&#x60;.  This is used to join a private lobby where the lobby code was shared to other users manually.</param>
        /// <param name="player">player param</param>
        [Preserve]
        public JoinByCodeRequest(string lobbyCode, Player player = default(Player))
        {
            LobbyCode = lobbyCode;
            Player = player;
        }

        /// <summary>
        /// The lobby code of the lobby the join.  Mutually exclusive with &#x60;id&#x60;.  This is used to join a private lobby where the lobby code was shared to other users manually.
        /// </summary>
        [Preserve]
        [DataMember(Name = "lobbyCode", IsRequired = true, EmitDefaultValue = true)]
        public string LobbyCode{ get; }

        /// <summary>
        /// player param
        /// </summary>
        [Preserve]
        [DataMember(Name = "player", EmitDefaultValue = false)]
        public Player Player{ get; }
    
    }
}

