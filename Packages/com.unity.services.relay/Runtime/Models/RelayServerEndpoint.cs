using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;



namespace Unity.Services.Relay.Models
{
    /// <summary>
    /// Endpoint connection details for a relay server
    /// </summary>
    /// <param name="connection_type">Canonical connection type</param>
    /// <param name="network">IP network (udp, tcp)</param>
    /// <param name="reliable">Is the delivery of data guaranteed</param>
    /// <param name="secure">Is the endpoint secured</param>
    /// <param name="host">Host name or address of the relay server</param>
    /// <param name="port">Port number</param>
    [Preserve]
    [DataContract(Name = "RelayServerEndpoint")]
    public class RelayServerEndpoint
    {
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

        [Preserve]
        [DataMember(Name = "connection_type", IsRequired = true, EmitDefaultValue = true)]
        public string ConnectionType{ get; }

        [Preserve]
        [JsonConverter(typeof(StringEnumConverter))]
        [DataMember(Name = "network", IsRequired = true, EmitDefaultValue = true)]
        public NetworkOptions Network{ get; }

        [Preserve]
        [DataMember(Name = "reliable", IsRequired = true, EmitDefaultValue = true)]
        public bool Reliable{ get; }

        [Preserve]
        [DataMember(Name = "secure", IsRequired = true, EmitDefaultValue = true)]
        public bool Secure{ get; }

        [Preserve]
        [DataMember(Name = "host", IsRequired = true, EmitDefaultValue = true)]
        public string Host{ get; }

        [Preserve]
        [DataMember(Name = "port", IsRequired = true, EmitDefaultValue = true)]
        public int Port{ get; }
    

        /// <summary>
        /// IP network (udp, tcp)
        /// </summary>
        /// <value>IP network (udp, tcp)</value>
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

