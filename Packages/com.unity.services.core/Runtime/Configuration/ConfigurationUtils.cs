using System.IO;
using UnityEngine;

namespace Unity.Services.Core.Configuration
{
    static class ConfigurationUtils
    {
        public const string StreamingAssetsFolder = "StreamingAssets";
        public const string StreamingAssetsPath = "Assets/" + StreamingAssetsFolder;
        public const string ConfigFileName = "UnityServicesProjectConfiguration.json";

        public static string RuntimeConfigFullPath { get; }
            = Path.Combine(Application.streamingAssetsPath, ConfigFileName);

        public static IConfigurationLoader ConfigurationLoader { get; internal set; }
            = new StreamingAssetsConfigurationLoader();
    }
}
