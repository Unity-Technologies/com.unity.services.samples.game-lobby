using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;



namespace Unity.Services.Relay.Models
{
    /// <summary>
    /// AllocationData model
    /// </summary>
    /// <param name="allocation">allocation param</param>
    [Preserve]
    [DataContract(Name = "AllocationData")]
    public class AllocationData
    {
        [Preserve]
        public AllocationData(Allocation allocation)
        {
            Allocation = allocation;
        }

        [Preserve]
        [DataMember(Name = "allocation", IsRequired = true, EmitDefaultValue = true)]
        public Allocation Allocation{ get; }
    
    }
}

