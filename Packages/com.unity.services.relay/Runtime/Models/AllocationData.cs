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
    /// AllocationData model
    /// <param name="allocation">allocation param</param>
    /// </summary>

    [Preserve]
    [DataContract(Name = "AllocationData")]
    public class AllocationData
    {
        /// <summary>
        /// Creates an instance of AllocationData.
        /// </summary>
        /// <param name="allocation">allocation param</param>
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

