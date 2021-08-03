using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Unity.Services.Lobbies.Http;



namespace Unity.Services.Lobbies.Models
{
    /// <summary>
    /// Custom data property for a lobby.
    /// <param name="value">The value of the custom property.  This property can be set to null or empty string.  If this property is indexed (by setting the &#x60;index&#x60; field) then the length of the value must be less than 128 bytes.</param>
    /// <param name="visibility">Indicates for whom the property should be visible.  If &#x60;public&#x60;, the property will be visible to everyone and will be included in query results.  If &#x60;member&#x60; the data will only be visible to users who are members of the lobby (i.e. those who have successfully joined).  If &#x60;private&#x60;, the metadata will only be visible to the host.</param>
    /// <param name="index">The name of the column to index this property value under, either &#x60;S#&#x60; for strings or &#x60;N#&#x60; for numeric values.  If an index is specified on a property, then you can use that index name in a &#x60;QueryFilter&#x60; to filter results by that property.  If   You will not be prevented from indexing multiple objects having properties different with names but the same the same index, but you will likely receive unexpected results from a query.</param>
    /// </summary>

    [Preserve]
    [DataContract(Name = "DataObject")]
    public class DataObject
    {
        /// <summary>
        /// Custom data property for a lobby.
        /// </summary>
        /// <param name="visibility">Indicates for whom the property should be visible.  If &#x60;public&#x60;, the property will be visible to everyone and will be included in query results.  If &#x60;member&#x60; the data will only be visible to users who are members of the lobby (i.e. those who have successfully joined).  If &#x60;private&#x60;, the metadata will only be visible to the host.</param>
        /// <param name="value">The value of the custom property.  This property can be set to null or empty string.  If this property is indexed (by setting the &#x60;index&#x60; field) then the length of the value must be less than 128 bytes.</param>
        /// <param name="index">The name of the column to index this property value under, either &#x60;S#&#x60; for strings or &#x60;N#&#x60; for numeric values.  If an index is specified on a property, then you can use that index name in a &#x60;QueryFilter&#x60; to filter results by that property.  If   You will not be prevented from indexing multiple objects having properties different with names but the same the same index, but you will likely receive unexpected results from a query.</param>
        [Preserve]
        public DataObject(VisibilityOptions visibility, string value = default, IndexOptions index = default)
        {
            Value = value;
            Visibility = visibility;
            Index = index;
        }

    
        /// <summary>
        /// The value of the custom property.  This property can be set to null or empty string.  If this property is indexed (by setting the &#x60;index&#x60; field) then the length of the value must be less than 128 bytes.
        /// </summary>
        [Preserve]
        [DataMember(Name = "value", EmitDefaultValue = false)]
        public string Value{ get; }

        /// <summary>
        /// Indicates for whom the property should be visible.  If &#x60;public&#x60;, the property will be visible to everyone and will be included in query results.  If &#x60;member&#x60; the data will only be visible to users who are members of the lobby (i.e. those who have successfully joined).  If &#x60;private&#x60;, the metadata will only be visible to the host.
        /// </summary>
        [Preserve]
        [JsonConverter(typeof(StringEnumConverter))]
        [DataMember(Name = "visibility", IsRequired = true, EmitDefaultValue = true)]
        public VisibilityOptions Visibility{ get; }

        /// <summary>
        /// The name of the column to index this property value under, either &#x60;S#&#x60; for strings or &#x60;N#&#x60; for numeric values.  If an index is specified on a property, then you can use that index name in a &#x60;QueryFilter&#x60; to filter results by that property.  If   You will not be prevented from indexing multiple objects having properties different with names but the same the same index, but you will likely receive unexpected results from a query.
        /// </summary>
        [Preserve]
        [JsonConverter(typeof(StringEnumConverter))]
        [DataMember(Name = "index", EmitDefaultValue = false)]
        public IndexOptions Index{ get; }
    

        /// <summary>
        /// Indicates for whom the property should be visible.  If &#x60;public&#x60;, the property will be visible to everyone and will be included in query results.  If &#x60;member&#x60; the data will only be visible to users who are members of the lobby (i.e. those who have successfully joined).  If &#x60;private&#x60;, the metadata will only be visible to the host.
        /// </summary>
        /// <value>Indicates for whom the property should be visible.  If &#x60;public&#x60;, the property will be visible to everyone and will be included in query results.  If &#x60;member&#x60; the data will only be visible to users who are members of the lobby (i.e. those who have successfully joined).  If &#x60;private&#x60;, the metadata will only be visible to the host.</value>
        [Preserve]
        [JsonConverter(typeof(StringEnumConverter))]
        public enum VisibilityOptions
        {
            /// <summary>
            /// Enum Public for value: public
            /// </summary>
            [EnumMember(Value = "public")]
            Public = 1,

            /// <summary>
            /// Enum Member for value: member
            /// </summary>
            [EnumMember(Value = "member")]
            Member = 2,

            /// <summary>
            /// Enum Private for value: private
            /// </summary>
            [EnumMember(Value = "private")]
            Private = 3

        }


        /// <summary>
        /// The name of the column to index this property value under, either &#x60;S#&#x60; for strings or &#x60;N#&#x60; for numeric values.  If an index is specified on a property, then you can use that index name in a &#x60;QueryFilter&#x60; to filter results by that property.  If   You will not be prevented from indexing multiple objects having properties different with names but the same the same index, but you will likely receive unexpected results from a query.
        /// </summary>
        /// <value>The name of the column to index this property value under, either &#x60;S#&#x60; for strings or &#x60;N#&#x60; for numeric values.  If an index is specified on a property, then you can use that index name in a &#x60;QueryFilter&#x60; to filter results by that property.  If   You will not be prevented from indexing multiple objects having properties different with names but the same the same index, but you will likely receive unexpected results from a query.</value>
        [Preserve]
        [JsonConverter(typeof(StringEnumConverter))]
        public enum IndexOptions
        {
            /// <summary>
            /// Enum S1 for value: S1
            /// </summary>
            [EnumMember(Value = "S1")]
            S1 = 1,

            /// <summary>
            /// Enum S2 for value: S2
            /// </summary>
            [EnumMember(Value = "S2")]
            S2 = 2,

            /// <summary>
            /// Enum S3 for value: S3
            /// </summary>
            [EnumMember(Value = "S3")]
            S3 = 3,

            /// <summary>
            /// Enum S4 for value: S4
            /// </summary>
            [EnumMember(Value = "S4")]
            S4 = 4,

            /// <summary>
            /// Enum S5 for value: S5
            /// </summary>
            [EnumMember(Value = "S5")]
            S5 = 5,

            /// <summary>
            /// Enum N1 for value: N1
            /// </summary>
            [EnumMember(Value = "N1")]
            N1 = 6,

            /// <summary>
            /// Enum N2 for value: N2
            /// </summary>
            [EnumMember(Value = "N2")]
            N2 = 7,

            /// <summary>
            /// Enum N3 for value: N3
            /// </summary>
            [EnumMember(Value = "N3")]
            N3 = 8,

            /// <summary>
            /// Enum N4 for value: N4
            /// </summary>
            [EnumMember(Value = "N4")]
            N4 = 9,

            /// <summary>
            /// Enum N5 for value: N5
            /// </summary>
            [EnumMember(Value = "N5")]
            N5 = 10

        }

    }
}

