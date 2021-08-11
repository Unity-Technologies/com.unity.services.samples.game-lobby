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
    /// Endpoint connection details for a relay server
    /// <param name="connectionType">Canonical connection type</param>
    /// <param name="network">IP network (udp, tcp)</param>
    /// <param name="reliable">Is the delivery of data guaranteed</param>
    /// <param name="secure">Is the endpoint secured</param>
    /// <param name="host">Host name or address of the relay server</param>
    /// <param name="port">Port number</param>
    /// </summary>

    [Preserve]
    [DataContract(Name = "RelayServerEndpoint")]
    public class RelayServerEndpoint
    {
        /// <summary>
        /// Endpoint connection details for a relay server
        /// </summary>
        /// <param name="connectionType">Canonical connection type</param>
        /// <param name="network">IP network (udp, tcp)</param>
        /// <param name="reliable">Is the delivery of data guaranteed</param>
        /// <param name="secure">Is the endpoint secured</param>
        /// <param name="host">Host name or address of the relay server</param>
        /// <param name="port">Port number</param>
        [Preserve]
        public RelayServerEndpoint(string connectionType, NetworkOptions network, bool reliable, bool secure, string host, int port)
        {
            ConnectionType = connectionType;
            Network = network;
            Reliable = reliable;
            Secure = secure;
            Host = host;
            Port = port;
        }

    
        /// <summary>
        /// Canonical connection type
        /// </summary>
        [Preserve]
        [DataMember(Name = "connectionType", IsRequired = true, EmitDefaultValue = true)]
        public string ConnectionType{ get; }

        /// <summary>
        /// IP network (udp, tcp)
        /// </summary>
        [Preserve]
        [JsonConverter(typeof(StringEnumConverter))]
        [DataMember(Name = "network", IsRequired = true, EmitDefaultValue = true)]
        public NetworkOptions Network{ get; }

        /// <summary>
        /// Is the delivery of data guaranteed
        /// </summary>
        [Preserve]
        [DataMember(Name = "reliable", IsRequired = true, EmitDefaultValue = true)]
        public bool Reliable{ get; }

        /// <summary>
        /// Is the endpoint secured
        /// </summary>
        [Preserve]
        [DataMember(Name = "secure", IsRequired = true, EmitDefaultValue = true)]
        public bool Secure{ get; }

        /// <summary>
        /// Host name or address of the relay server
        /// </summary>
        [Preserve]
        [DataMember(Name = "host", IsRequired = true, EmitDefaultValue = true)]
        public string Host{ get; }

        /// <summary>
        /// Port number
        /// </summary>
        [Preserve]
        [DataMember(Name = "port", IsRequired = true, EmitDefaultValue = true)]
        public int Port{ get; }
    

        /// <summary>
        /// IP network (udp, tcp)
        /// </summary>
        /// <value>IP network (udp, tcp)</value>
        [Preserve]
        [JsonConverter(typeof(StringEnumConverter))]
        public enum NetworkOptions
        {
            /// <summary>
            /// Enum Udp for value: udp
            /// </summary>
            [EnumMember(Value = "udp")]
            Udp = 1,

            /// <summary>
            /// Enum Tcp for value: tcp
            /// </summary>
            [EnumMember(Value = "tcp")]
            Tcp = 2

        }

    }
}

