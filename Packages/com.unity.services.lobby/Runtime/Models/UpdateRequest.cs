using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;



namespace Unity.Services.Lobbies.Models
{
    /// <summary>
    /// The body of an Update Lobby request.
    /// </summary>
    [Preserve]
    [DataContract(Name = "UpdateRequest")]
    public class UpdateRequest
    {
        /// <summary>
        /// The body of an Update Lobby request.
        /// </summary>
        /// <param name="name">The name of the lobby that should be displayed to users.  All whitespace will be trimmed from name.</param>
        /// <param name="maxPlayers">The maximum number of players allowed in the lobby.  Must be greater than or equal to the current number of players in the lobby.</param>
        /// <param name="isPrivate">Indicates whether or not the lobby is publicly visible and will show up in query results.  If the lobby is not publicly visible, the creator can share the &#x60;lobbyCode&#x60; with other users who can use it to join this lobby.</param>
        /// <param name="data">Custom game-specific properties to add, update, or remove from the lobby (e.g. &#x60;mapName&#x60; or &#x60;gameType&#x60;).  To remove an existing property, include it in &#x60;data&#x60; but set the property object to &#x60;null&#x60;.  To update the value to &#x60;null&#x60;, set the &#x60;value&#x60; property of the object to &#x60;null&#x60;.</param>
        /// <param name="hostId">The id of the player to make the host of the lobby.  As soon as this is updated the current host will no longer have permission to modify the lobby.</param>
        [Preserve]
        public UpdateRequest(string name = null, int? maxPlayers = null, bool? isPrivate = null, Dictionary<string, DataObject> data = null, string hostId = null)
        {
            Name = name;
            MaxPlayers = maxPlayers;
            IsPrivate = isPrivate;
            Data = data;
            HostId = hostId;
        }

        /// <summary>
        /// The name of the lobby that should be displayed to users.  All whitespace will be trimmed from name.
        /// </summary>
        [Preserve]
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name{ get; }

        /// <summary>
        /// The maximum number of players allowed in the lobby.  Must be greater than or equal to the current number of players in the lobby.
        /// </summary>
        [Preserve]
        [DataMember(Name = "maxPlayers", EmitDefaultValue = false)]
        public int? MaxPlayers{ get; }

        /// <summary>
        /// Indicates whether or not the lobby is publicly visible and will show up in query results.  If the lobby is not publicly visible, the creator can share the &#x60;lobbyCode&#x60; with other users who can use it to join this lobby.
        /// </summary>
        [Preserve]
        [DataMember(Name = "isPrivate", EmitDefaultValue = true)]
        public bool? IsPrivate{ get; }

        /// <summary>
        /// Custom game-specific properties to add, update, or remove from the lobby (e.g. &#x60;mapName&#x60; or &#x60;gameType&#x60;).  To remove an existing property, include it in &#x60;data&#x60; but set the property object to &#x60;null&#x60;.  To update the value to &#x60;null&#x60;, set the &#x60;value&#x60; property of the object to &#x60;null&#x60;.
        /// </summary>
        [Preserve]
        [DataMember(Name = "data", EmitDefaultValue = false)]
        public Dictionary<string, DataObject> Data{ get; }

        /// <summary>
        /// The id of the player to make the host of the lobby.  As soon as this is updated the current host will no longer have permission to modify the lobby.
        /// </summary>
        [Preserve]
        [DataMember(Name = "hostId", EmitDefaultValue = false)]
        public string HostId{ get; }
    
    }
}

