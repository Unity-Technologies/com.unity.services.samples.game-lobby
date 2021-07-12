using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;



namespace Unity.Services.Relay.Models
{
    /// <summary>
    /// JoinCodeData model
    /// </summary>
    /// <param name="join_code">Join code associated with the given connection data</param>
    [Preserve]
    [DataContract(Name = "JoinCodeData")]
    public class JoinCodeData
    {
        [Preserve]
        public JoinCodeData(string joinCode)
        {
            JoinCode = joinCode;
        }

        [Preserve]
        [DataMember(Name = "join_code", IsRequired = true, EmitDefaultValue = true)]
        public string JoinCode{ get; }
    
    }
}

