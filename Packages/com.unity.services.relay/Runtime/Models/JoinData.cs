using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;



namespace Unity.Services.Relay.Models
{
    /// <summary>
    /// JoinData model
    /// </summary>
    /// <param name="allocation">allocation param</param>
    [Preserve]
    [DataContract(Name = "JoinData")]
    public class JoinData
    {
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

