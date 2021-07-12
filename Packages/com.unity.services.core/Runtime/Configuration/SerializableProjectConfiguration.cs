using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Services.Core.Configuration
{
    [Serializable]
    struct SerializableProjectConfiguration
    {
        public static SerializableProjectConfiguration Empty
            => new SerializableProjectConfiguration
        {
            Keys = new string[0],
            Values = new ConfigurationEntry[0],
        };

        [SerializeField]
        internal string[] Keys;

        [SerializeField]
        internal ConfigurationEntry[] Values;

        public SerializableProjectConfiguration(IDictionary<string, ConfigurationEntry> configValues)
        {
            Keys = new string[configValues.Count];
            Values = new ConfigurationEntry[configValues.Count];

            var i = 0;
            foreach (var configValue in configValues)
            {
                Keys[i] = configValue.Key;
                Values[i] = configValue.Value;
                ++i;
            }
        }
    }
}
