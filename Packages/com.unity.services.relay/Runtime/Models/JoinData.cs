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
    /// JoinData model
    /// <param name="allocation">allocation param</param>
    /// </summary>

    [Preserve]
    [DataContract(Name = "JoinData")]
    public class JoinData
    {
        /// <summary>
        /// Creates an instance of JoinData.
        /// </summary>
        /// <param name="allocation">allocation param</param>
        [Preserve]
        public JoinData(JoinAllocation allocation)
        {
            Allocation = allocation;
        }

    
        [Preserve]
        [DataMember(Name = "allocation", IsRequired = true, EmitDefaultValue = true)]
        public JoinAllocation Allocation{ get; }
    
    }
}

