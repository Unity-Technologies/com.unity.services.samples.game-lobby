using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Services.Core.Configuration
{
    class RuntimeConfigurationLoader : IConfigurationLoader
    {
        public async Task<SerializableProjectConfiguration> GetConfigAsync()
        {
            var jsonConfig = await StreamingAssetsUtils.GetFileTextFromStreamingAssetsAsync(
                ConfigurationUtils.ConfigFileName);
            var config = JsonUtility.FromJson<SerializableProjectConfiguration>(jsonConfig);
            return config;
        }
    }
}
