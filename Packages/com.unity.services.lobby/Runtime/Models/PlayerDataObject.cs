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
    /// Custom data property for a player.
    /// <param name="value">The value of the custom property.  This property can be set to null or empty string.</param>
    /// <param name="visibility">Indicates for whom the property should be visible.  If &#x60;public&#x60;, the property will be visible to everyone and will be included in query results.  If &#x60;member&#x60; the data will only be visible to users who are members of the lobby (i.e. those who have successfully joined).  If &#x60;private&#x60;, the metadata will only be visible to the the player.</param>
    /// </summary>

    [Preserve]
    [DataContract(Name = "PlayerDataObject")]
    public class PlayerDataObject
    {
        /// <summary>
        /// Custom data property for a player.
        /// </summary>
        /// <param name="visibility">Indicates for whom the property should be visible.  If &#x60;public&#x60;, the property will be visible to everyone and will be included in query results.  If &#x60;member&#x60; the data will only be visible to users who are members of the lobby (i.e. those who have successfully joined).  If &#x60;private&#x60;, the metadata will only be visible to the the player.</param>
        /// <param name="value">The value of the custom property.  This property can be set to null or empty string.</param>
        [Preserve]
        public PlayerDataObject(VisibilityOptions visibility, string value = default)
        {
            Value = value;
            Visibility = visibility;
        }

    
        /// <summary>
        /// The value of the custom property.  This property can be set to null or empty string.
        /// </summary>
        [Preserve]
        [DataMember(Name = "value", EmitDefaultValue = false)]
        public string Value{ get; }

        /// <summary>
        /// Indicates for whom the property should be visible.  If &#x60;public&#x60;, the property will be visible to everyone and will be included in query results.  If &#x60;member&#x60; the data will only be visible to users who are members of the lobby (i.e. those who have successfully joined).  If &#x60;private&#x60;, the metadata will only be visible to the the player.
        /// </summary>
        [Preserve]
        [JsonConverter(typeof(StringEnumConverter))]
        [DataMember(Name = "visibility", IsRequired = true, EmitDefaultValue = true)]
        public VisibilityOptions Visibility{ get; }
    

        /// <summary>
        /// Indicates for whom the property should be visible.  If &#x60;public&#x60;, the property will be visible to everyone and will be included in query results.  If &#x60;member&#x60; the data will only be visible to users who are members of the lobby (i.e. those who have successfully joined).  If &#x60;private&#x60;, the metadata will only be visible to the the player.
        /// </summary>
        /// <value>Indicates for whom the property should be visible.  If &#x60;public&#x60;, the property will be visible to everyone and will be included in query results.  If &#x60;member&#x60; the data will only be visible to users who are members of the lobby (i.e. those who have successfully joined).  If &#x60;private&#x60;, the metadata will only be visible to the the player.</value>
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

    }
}

