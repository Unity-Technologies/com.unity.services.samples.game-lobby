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
    /// A list of lobbies that matched the specified lobbies query. Only the public top level, data, and player data properties are returned.
    /// <param name="results">results param</param>
    /// <param name="continuationToken">continuationToken param</param>
    /// </summary>

    [Preserve]
    [DataContract(Name = "QueryResponse")]
    public class QueryResponse
    {
        /// <summary>
        /// A list of lobbies that matched the specified lobbies query. Only the public top level, data, and player data properties are returned.
        /// </summary>
        /// <param name="results">results param</param>
        /// <param name="continuationToken">continuationToken param</param>
        [Preserve]
        public QueryResponse(List<Lobby> results = default, string continuationToken = default)
        {
            Results = results;
            ContinuationToken = continuationToken;
        }

    
        [Preserve]
        [DataMember(Name = "results", EmitDefaultValue = false)]
        public List<Lobby> Results{ get; }

        [Preserve]
        [DataMember(Name = "continuationToken", EmitDefaultValue = false)]
        public string ContinuationToken{ get; }
    
    }
}

