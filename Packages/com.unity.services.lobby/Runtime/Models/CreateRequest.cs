using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Unity.Services.Lobbies.Http;



namespace Unity.Services.Lobbies.Models
{
    /// <summary>
    /// The body of a Create Lobby request.
    /// <param name="name">The name of the lobby that should be displayed to users.  All whitespace will be trimmed from name.</param>
    /// <param name="maxPlayers">The maximum number of players allowed in the lobby.</param>
    /// <param name="isPrivate">Indicates whether or not the lobby is publicly visible and will show up in query results.  If the lobby is not publicly visible, the creator can share the &#x60;lobbyCode&#x60; with other users who can use it to join this lobby.</param>
    /// <param name="player">player param</param>
    /// <param name="data">Custom game-specific properties that apply to the lobby (e.g. &#x60;mapName&#x60; or &#x60;gameType&#x60;).</param>
    /// </summary>

    [Preserve]
    [DataContract(Name = "CreateRequest")]
    public class CreateRequest
    {
        /// <summary>
        /// The body of a Create Lobby request.
        /// </summary>
        /// <param name="name">The name of the lobby that should be displayed to users.  All whitespace will be trimmed from name.</param>
        /// <param name="maxPlayers">The maximum number of players allowed in the lobby.</param>
        /// <param name="isPrivate">Indicates whether or not the lobby is publicly visible and will show up in query results.  If the lobby is not publicly visible, the creator can share the &#x60;lobbyCode&#x60; with other users who can use it to join this lobby.</param>
        /// <param name="player">player param</param>
        /// <param name="data">Custom game-specific properties that apply to the lobby (e.g. &#x60;mapName&#x60; or &#x60;gameType&#x60;).</param>
        [Preserve]
        public CreateRequest(string name, int maxPlayers, bool? isPrivate = false, Player player = default, Dictionary<string, DataObject> data = default)
        {
            Name = name;
            MaxPlayers = maxPlayers;
            IsPrivate = isPrivate;
            Player = player;
            Data = data;
        }

    
        /// <summary>
        /// The name of the lobby that should be displayed to users.  All whitespace will be trimmed from name.
        /// </summary>
        [Preserve]
        [DataMember(Name = "name", IsRequired = true, EmitDefaultValue = true)]
        public string Name{ get; }

        /// <summary>
        /// The maximum number of players allowed in the lobby.
        /// </summary>
        [Preserve]
        [DataMember(Name = "maxPlayers", IsRequired = true, EmitDefaultValue = true)]
        public int MaxPlayers{ get; }

        /// <summary>
        /// Indicates whether or not the lobby is publicly visible and will show up in query results.  If the lobby is not publicly visible, the creator can share the &#x60;lobbyCode&#x60; with other users who can use it to join this lobby.
        /// </summary>
        [Preserve]
        [DataMember(Name = "isPrivate", EmitDefaultValue = true)]
        public bool? IsPrivate{ get; }

        [Preserve]
        [DataMember(Name = "player", EmitDefaultValue = false)]
        public Player Player{ get; }

        /// <summary>
        /// Custom game-specific properties that apply to the lobby (e.g. &#x60;mapName&#x60; or &#x60;gameType&#x60;).
        /// </summary>
        [Preserve]
        [DataMember(Name = "data", EmitDefaultValue = false)]
        public Dictionary<string, DataObject> Data{ get; }
    
    }
}

