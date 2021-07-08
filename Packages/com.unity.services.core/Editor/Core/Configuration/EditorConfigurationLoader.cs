using System.Threading.Tasks;

namespace Unity.Services.Core.Configuration.Editor
{
    class EditorConfigurationLoader : IConfigurationLoader
    {
        internal SerializableProjectConfiguration PlayModeConfig { get; set; }

        Task<SerializableProjectConfiguration> IConfigurationLoader.GetConfigAsync()
        {
            var completionSource = new TaskCompletionSource<SerializableProjectConfiguration>();
            completionSource.SetResult(PlayModeConfig);
            return completionSource.Task;
        }
    }
}
