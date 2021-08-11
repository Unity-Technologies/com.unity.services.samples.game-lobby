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
    /// RegionsData model
    /// <param name="regions">Regions where relay servers might be available</param>
    /// </summary>

    [Preserve]
    [DataContract(Name = "RegionsData")]
    public class RegionsData
    {
        /// <summary>
        /// Creates an instance of RegionsData.
        /// </summary>
        /// <param name="regions">Regions where relay servers might be available</param>
        [Preserve]
        public RegionsData(List<Region> regions)
        {
            Regions = regions;
        }

    
        /// <summary>
        /// Regions where relay servers might be available
        /// </summary>
        [Preserve]
        [DataMember(Name = "regions", IsRequired = true, EmitDefaultValue = true)]
        public List<Region> Regions{ get; }
    
    }
}

