using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;



namespace Unity.Services.Lobbies.Models
{
    /// <summary>
    /// An order for an individual field that is applied to a query.
    /// </summary>
    [Preserve]
    [DataContract(Name = "QueryOrder")]
    public class QueryOrder
    {
        /// <summary>
        /// An order for an individual field that is applied to a query.
        /// </summary>
        /// <param name="asc">Whether to sort in ascending or descending order.</param>
        /// <param name="field">The name of the field to order on.</param>
        [Preserve]
        public QueryOrder(bool asc = default(bool), FieldOptions field = default)
        {
            Asc = asc;
            Field = field;
        }

        /// <summary>
        /// Whether to sort in ascending or descending order.
        /// </summary>
        [Preserve]
        [DataMember(Name = "asc", EmitDefaultValue = true)]
        public bool Asc{ get; }

        /// <summary>
        /// The name of the field to order on.
        /// </summary>
        [Preserve]
        [JsonConverter(typeof(StringEnumConverter))]
        [DataMember(Name = "field", EmitDefaultValue = false)]
        public FieldOptions Field{ get; }
    

        /// <summary>
        /// The name of the field to order on.
        /// </summary>
        /// <value>The name of the field to order on.</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum FieldOptions
        {
            /// <summary>
            /// Enum Name for value: Name
            /// </summary>
            [EnumMember(Value = "Name")]
            Name = 1,

            /// <summary>
            /// Enum MaxPlayers for value: MaxPlayers
            /// </summary>
            [EnumMember(Value = "MaxPlayers")]
            MaxPlayers = 2,

            /// <summary>
            /// Enum AvailableSlots for value: AvailableSlots
            /// </summary>
            [EnumMember(Value = "AvailableSlots")]
            AvailableSlots = 3,

            /// <summary>
            /// Enum Created for value: Created
            /// </summary>
            [EnumMember(Value = "Created")]
            Created = 4,

            /// <summary>
            /// Enum LastUpdated for value: LastUpdated
            /// </summary>
            [EnumMember(Value = "LastUpdated")]
            LastUpdated = 5,

            /// <summary>
            /// Enum ID for value: ID
            /// </summary>
            [EnumMember(Value = "ID")]
            ID = 6,

            /// <summary>
            /// Enum S1 for value: S1
            /// </summary>
            [EnumMember(Value = "S1")]
            S1 = 7,

            /// <summary>
            /// Enum S2 for value: S2
            /// </summary>
            [EnumMember(Value = "S2")]
            S2 = 8,

            /// <summary>
            /// Enum S3 for value: S3
            /// </summary>
            [EnumMember(Value = "S3")]
            S3 = 9,

            /// <summary>
            /// Enum S4 for value: S4
            /// </summary>
            [EnumMember(Value = "S4")]
            S4 = 10,

            /// <summary>
            /// Enum S5 for value: S5
            /// </summary>
            [EnumMember(Value = "S5")]
            S5 = 11,

            /// <summary>
            /// Enum N1 for value: N1
            /// </summary>
            [EnumMember(Value = "N1")]
            N1 = 12,

            /// <summary>
            /// Enum N2 for value: N2
            /// </summary>
            [EnumMember(Value = "N2")]
            N2 = 13,

            /// <summary>
            /// Enum N3 for value: N3
            /// </summary>
            [EnumMember(Value = "N3")]
            N3 = 14,

            /// <summary>
            /// Enum N4 for value: N4
            /// </summary>
            [EnumMember(Value = "N4")]
            N4 = 15,

            /// <summary>
            /// Enum N5 for value: N5
            /// </summary>
            [EnumMember(Value = "N5")]
            N5 = 16

        }

    }
}

