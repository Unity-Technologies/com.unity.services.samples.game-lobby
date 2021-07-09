using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;



namespace Unity.Services.Lobbies.Models
{
    /// <summary>
    /// The body of an Update Player Data request.
    /// </summary>
    [Preserve]
    [DataContract(Name = "PlayerUpdateRequest")]
    public class PlayerUpdateRequest
    {
        /// <summary>
        /// The body of an Update Player Data request.
        /// </summary>
        /// <param name="connectionInfo">(TBD) Connection information for connecting to a relay with this player.</param>
        /// <param name="data">Custom game-specific properties to add, update, or remove from the player (e.g. &#x60;role&#x60; or &#x60;skill&#x60;). To remove an existing property, include it in &#x60;data&#x60; but set the property object to &#x60;null&#x60;.  To update the value to &#x60;null&#x60;, set the &#x60;value&#x60; property of the object to &#x60;null&#x60;.</param>
        /// <param name="allocationId">An id that associates this player in this lobby with a persistent connection.  When a disconnect notification is recevied, this value is used to identify the associated player in a lobby to mark them as disconnected.</param>
        [Preserve]
        public PlayerUpdateRequest(string connectionInfo = null, Dictionary<string, PlayerDataObject> data = null, string allocationId = null)
        {
            ConnectionInfo = connectionInfo;
            Data = data;
            AllocationId = allocationId;
        }

        /// <summary>
        /// (TBD) Connection information for connecting to a relay with this player.
        /// </summary>
        [Preserve]
        [DataMember(Name = "connectionInfo", EmitDefaultValue = false)]
        public string ConnectionInfo{ get; }

        /// <summary>
        /// Custom game-specific properties to add, update, or remove from the player (e.g. &#x60;role&#x60; or &#x60;skill&#x60;). To remove an existing property, include it in &#x60;data&#x60; but set the property object to &#x60;null&#x60;.  To update the value to &#x60;null&#x60;, set the &#x60;value&#x60; property of the object to &#x60;null&#x60;.
        /// </summary>
        [Preserve]
        [DataMember(Name = "data", EmitDefaultValue = false)]
        public Dictionary<string, PlayerDataObject> Data{ get; }

        /// <summary>
        /// An id that associates this player in this lobby with a persistent connection.  When a disconnect notification is recevied, this value is used to identify the associated player in a lobby to mark them as disconnected.
        /// </summary>
        [Preserve]
        [DataMember(Name = "allocationId", EmitDefaultValue = false)]
        public string AllocationId{ get; }
    
    }
}

