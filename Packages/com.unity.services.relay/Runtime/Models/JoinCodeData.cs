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
    /// JoinCodeData model
    /// <param name="joinCode">The join code is used to lookup the connection data for the player responsible for creating it. It is case-insensitive.  The connection data is returned to the caller and can be used in the request to the \&quot;join\&quot; endpoint to join a relay server.</param>
    /// </summary>

    [Preserve]
    [DataContract(Name = "JoinCodeData")]
    public class JoinCodeData
    {
        /// <summary>
        /// Creates an instance of JoinCodeData.
        /// </summary>
        /// <param name="joinCode">The join code is used to lookup the connection data for the player responsible for creating it. It is case-insensitive.  The connection data is returned to the caller and can be used in the request to the \&quot;join\&quot; endpoint to join a relay server.</param>
        [Preserve]
        public JoinCodeData(string joinCode)
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

