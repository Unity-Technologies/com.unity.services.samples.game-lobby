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
    /// A request to join a relay using a join code
    /// <param name="joinCode">The join code is used to lookup the connection data for the player responsible for creating it. It is case-insensitive.  The connection data is returned to the caller and can be used in the request to the \&quot;join\&quot; endpoint to join a relay server.</param>
    /// </summary>

    [Preserve]
    [DataContract(Name = "JoinRequest")]
    public class JoinRequest
    {
        /// <summary>
        /// A request to join a relay using a join code
        /// </summary>
        /// <param name="joinCode">The join code is used to lookup the connection data for the player responsible for creating it. It is case-insensitive.  The connection data is returned to the caller and can be used in the request to the \&quot;join\&quot; endpoint to join a relay server.</param>
        [Preserve]
        public JoinRequest(string joinCode)
        {
            JoinCode = joinCode;
        }

    
        /// <summary>
        /// The join code is used to lookup the connection data for the player responsible for creating it. It is case-insensitive.  The connection data is returned to the caller and can be used in the request to the \&quot;join\&quot; endpoint to join a relay server.
        /// </summary>
        [Preserve]
        [DataMember(Name = "joinCode", IsRequired = true, EmitDefaultValue = true)]
        public string JoinCode{ get; }
    
    }
}

