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
    /// The body of a QuickJoin request.
    /// <param name="filter">A list of filters which can be used to narrow down which lobbies to attempt to join..</param>
    /// <param name="player">player param</param>
    /// </summary>

    [Preserve]
    [DataContract(Name = "QuickJoinRequest")]
    public class QuickJoinRequest
    {
        /// <summary>
        /// The body of a QuickJoin request.
        /// </summary>
        /// <param name="filter">A list of filters which can be used to narrow down which lobbies to attempt to join..</param>
        /// <param name="player">player param</param>
        [Preserve]
        public QuickJoinRequest(List<QueryFilter> filter = default, Player player = default)
        {
            Filter = filter;
            Player = player;
        }

    
        /// <summary>
        /// A list of filters which can be used to narrow down which lobbies to attempt to join..
        /// </summary>
        [Preserve]
        [DataMember(Name = "filter", EmitDefaultValue = false)]
        public List<QueryFilter> Filter{ get; }

        [Preserve]
        [DataMember(Name = "player", EmitDefaultValue = false)]
        public Player Player{ get; }
    
    }
}

