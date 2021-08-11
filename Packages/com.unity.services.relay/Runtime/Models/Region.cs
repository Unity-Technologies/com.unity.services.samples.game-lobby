using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Unity.Services.Relay.Http;



namespace Unity.Services.Relay.Models
{
    /// <summary>
    /// Region model
    /// <param name="id">The region id used in allocation requests</param>
    /// <param name="description">A human readable description of the region. May include geographical information, for example, city name, country.</param>
    /// </summary>

    [Preserve]
    [DataContract(Name = "Region")]
    public class Region
    {
        /// <summary>
        /// Creates an instance of Region.
        /// </summary>
        /// <param name="id">The region id used in allocation requests</param>
        /// <param name="description">A human readable description of the region. May include geographical information, for example, city name, country.</param>
        [Preserve]
        public Region(string id, string description)
        {
            Id = id;
            Description = description;
        }

    
        /// <summary>
        /// The region id used in allocation requests
        /// </summary>
        [Preserve]
        [DataMember(Name = "id", IsRequired = true, EmitDefaultValue = true)]
        public string Id{ get; }

        /// <summary>
        /// A human readable description of the region. May include geographical information, for example, city name, country.
        /// </summary>
        [Preserve]
        [DataMember(Name = "description", IsRequired = true, EmitDefaultValue = true)]
        public string Description{ get; }
    
    }
}

