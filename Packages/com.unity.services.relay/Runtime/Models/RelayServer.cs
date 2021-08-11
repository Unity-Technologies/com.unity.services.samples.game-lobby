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
    /// Deprecated: IPv4 connection details for a relay server. The network protocol (currently supported options are tcp and udp) required by this IP/Port is determined by the relay server configuration and is not indicated here. Prefer the \&quot;relay server endpoint\&quot; collection to see IP/Port combinations with the network protocol required.
    /// <param name="ipV4">IP (v4) address of the relay server</param>
    /// <param name="port">Port of the relay server</param>
    /// </summary>

    [Preserve]
    [DataContract(Name = "RelayServer")]
    public class RelayServer
    {
        /// <summary>
        /// Deprecated: IPv4 connection details for a relay server. The network protocol (currently supported options are tcp and udp) required by this IP/Port is determined by the relay server configuration and is not indicated here. Prefer the \&quot;relay server endpoint\&quot; collection to see IP/Port combinations with the network protocol required.
        /// </summary>
        /// <param name="ipV4">IP (v4) address of the relay server</param>
        /// <param name="port">Port of the relay server</param>
        [Preserve]
        public RelayServer(string ipV4, int port)
        {
            IpV4 = ipV4;
            Port = port;
        }

    
        /// <summary>
        /// IP (v4) address of the relay server
        /// </summary>
        [Preserve]
        [DataMember(Name = "ipV4", IsRequired = true, EmitDefaultValue = true)]
        public string IpV4{ get; }

        /// <summary>
        /// Port of the relay server
        /// </summary>
        [Preserve]
        [DataMember(Name = "port", IsRequired = true, EmitDefaultValue = true)]
        public int Port{ get; }
    
    }
}

