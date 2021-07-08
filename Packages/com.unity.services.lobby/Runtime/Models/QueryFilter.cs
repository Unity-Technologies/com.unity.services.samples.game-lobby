using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;



namespace Unity.Services.Lobbies.Models
{
    /// <summary>
    /// A filter for an individual field that is applied to a query.
    /// </summary>
    [Preserve]
    [DataContract(Name = "QueryFilter")]
    public class QueryFilter
    {
        /// <summary>
        /// A filter for an individual field that is applied to a query.
        /// </summary>
        /// <param name="field">The name of the field to filter on.  For custom data fields, the name of the index must be used instead of the field name.</param>
        /// <param name="value">The value to compare to the field being filtered.  This value must be a string and it must be parsable as the same type as &#x60;field&#x60; (e.g. &#x60;integer&#x60; for MaxPlayers, &#x60;datetime&#x60; for Created, etc.). The value for &#x60;datetime&#x60; fields (Created, LastUpdated) must be in RFC3339 format. For example, in C# this can be achieved using the \&quot;o\&quot; format specifier: &#x60;return dateTime.ToString(\&quot;o\&quot;, DateTimeFormatInfo.InvariantInfo);&#x60;. Refer to your language documentation for other methods to generate RFC3339-compatible datetime strings.</param>
        /// <param name="op">The operator used to compare the field to the filter value.  Supports &#x60;CONTAINS&#x60; (only on the &#x60;Name&#x60; field), &#x60;EQ&#x60; (Equal), &#x60;NE&#x60; (Not Equal), &#x60;LT&#x60; (Less Than), &#x60;LE&#x60; (Less Than or Equal), &#x60;GT&#x60; (Greater Than), or &#x60;GE&#x60; (Greater Than or Equal).</param>
        [Preserve]
        public QueryFilter(FieldOptions field, string value, OpOptions op)
        {
            Field = field;
            Value = value;
            Op = op;
        }

        /// <summary>
        /// The name of the field to filter on.  For custom data fields, the name of the index must be used instead of the field name.
        /// </summary>
        [Preserve]
        [JsonConverter(typeof(StringEnumConverter))]
        [DataMember(Name = "field", IsRequired = true, EmitDefaultValue = true)]
        public FieldOptions Field{ get; }

        /// <summary>
        /// The value to compare to the field being filtered.  This value must be a string and it must be parsable as the same type as &#x60;field&#x60; (e.g. &#x60;integer&#x60; for MaxPlayers, &#x60;datetime&#x60; for Created, etc.). The value for &#x60;datetime&#x60; fields (Created, LastUpdated) must be in RFC3339 format. For example, in C# this can be achieved using the \&quot;o\&quot; format specifier: &#x60;return dateTime.ToString(\&quot;o\&quot;, DateTimeFormatInfo.InvariantInfo);&#x60;. Refer to your language documentation for other methods to generate RFC3339-compatible datetime strings.
        /// </summary>
        [Preserve]
        [DataMember(Name = "value", IsRequired = true, EmitDefaultValue = true)]
        public string Value{ get; }

        /// <summary>
        /// The operator used to compare the field to the filter value.  Supports &#x60;CONTAINS&#x60; (only on the &#x60;Name&#x60; field), &#x60;EQ&#x60; (Equal), &#x60;NE&#x60; (Not Equal), &#x60;LT&#x60; (Less Than), &#x60;LE&#x60; (Less Than or Equal), &#x60;GT&#x60; (Greater Than), or &#x60;GE&#x60; (Greater Than or Equal).
        /// </summary>
        [Preserve]
        [JsonConverter(typeof(StringEnumConverter))]
        [DataMember(Name = "op", IsRequired = true, EmitDefaultValue = true)]
        public OpOptions Op{ get; }
    

        /// <summary>
        /// The name of the field to filter on.  For custom data fields, the name of the index must be used instead of the field name.
        /// </summary>
        /// <value>The name of the field to filter on.  For custom data fields, the name of the index must be used instead of the field name.</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum FieldOptions
        {
            /// <summary>
            /// Enum MaxPlayers for value: MaxPlayers
            /// </summary>
            [EnumMember(Value = "MaxPlayers")]
            MaxPlayers = 1,

            /// <summary>
            /// Enum AvailableSlots for value: AvailableSlots
            /// </summary>
            [EnumMember(Value = "AvailableSlots")]
            AvailableSlots = 2,

            /// <summary>
            /// Enum Name for value: Name
            /// </summary>
            [EnumMember(Value = "Name")]
            Name = 3,

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
            /// Enum S1 for value: S1
            /// </summary>
            [EnumMember(Value = "S1")]
            S1 = 6,

            /// <summary>
            /// Enum S2 for value: S2
            /// </summary>
            [EnumMember(Value = "S2")]
            S2 = 7,

            /// <summary>
            /// Enum S3 for value: S3
            /// </summary>
            [EnumMember(Value = "S3")]
            S3 = 8,

            /// <summary>
            /// Enum S4 for value: S4
            /// </summary>
            [EnumMember(Value = "S4")]
            S4 = 9,

            /// <summary>
            /// Enum S5 for value: S5
            /// </summary>
            [EnumMember(Value = "S5")]
            S5 = 10,

            /// <summary>
            /// Enum N1 for value: N1
            /// </summary>
            [EnumMember(Value = "N1")]
            N1 = 11,

            /// <summary>
            /// Enum N2 for value: N2
            /// </summary>
            [EnumMember(Value = "N2")]
            N2 = 12,

            /// <summary>
            /// Enum N3 for value: N3
            /// </summary>
            [EnumMember(Value = "N3")]
            N3 = 13,

            /// <summary>
            /// Enum N4 for value: N4
            /// </summary>
            [EnumMember(Value = "N4")]
            N4 = 14,

            /// <summary>
            /// Enum N5 for value: N5
            /// </summary>
            [EnumMember(Value = "N5")]
            N5 = 15

        }


        /// <summary>
        /// The operator used to compare the field to the filter value.  Supports &#x60;CONTAINS&#x60; (only on the &#x60;Name&#x60; field), &#x60;EQ&#x60; (Equal), &#x60;NE&#x60; (Not Equal), &#x60;LT&#x60; (Less Than), &#x60;LE&#x60; (Less Than or Equal), &#x60;GT&#x60; (Greater Than), or &#x60;GE&#x60; (Greater Than or Equal).
        /// </summary>
        /// <value>The operator used to compare the field to the filter value.  Supports &#x60;CONTAINS&#x60; (only on the &#x60;Name&#x60; field), &#x60;EQ&#x60; (Equal), &#x60;NE&#x60; (Not Equal), &#x60;LT&#x60; (Less Than), &#x60;LE&#x60; (Less Than or Equal), &#x60;GT&#x60; (Greater Than), or &#x60;GE&#x60; (Greater Than or Equal).</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum OpOptions
        {
            /// <summary>
            /// Enum CONTAINS for value: CONTAINS
            /// </summary>
            [EnumMember(Value = "CONTAINS")]
            CONTAINS = 1,

            /// <summary>
            /// Enum EQ for value: EQ
            /// </summary>
            [EnumMember(Value = "EQ")]
            EQ = 2,

            /// <summary>
            /// Enum NE for value: NE
            /// </summary>
            [EnumMember(Value = "NE")]
            NE = 3,

            /// <summary>
            /// Enum LT for value: LT
            /// </summary>
            [EnumMember(Value = "LT")]
            LT = 4,

            /// <summary>
            /// Enum LE for value: LE
            /// </summary>
            [EnumMember(Value = "LE")]
            LE = 5,

            /// <summary>
            /// Enum GT for value: GT
            /// </summary>
            [EnumMember(Value = "GT")]
            GT = 6,

            /// <summary>
            /// Enum GE for value: GE
            /// </summary>
            [EnumMember(Value = "GE")]
            GE = 7

        }

    }
}

