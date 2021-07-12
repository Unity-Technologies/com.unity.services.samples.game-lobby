using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;



namespace Unity.Services.Relay.Models
{
    /// <summary>
    /// A request to join a relay using a join code
    /// </summary>
    /// <param name="join_code">The join code is used to lookup the connection data for the player responsible for creating it.  The connection data is returned to the caller and can be used to establish communication.</param>
    [Preserve]
    [DataContract(Name = "JoinRequest")]
    public class JoinRequest
    {
        [Preserve]
        public JoinRequest(string joinCode)
        {
            JoinCode = joinCode;
        }

        [Preserve]
        [DataMember(Name = "join_code", IsRequired = true, EmitDefaultValue = true)]
        public string JoinCode{ get; }
    
    }
}

