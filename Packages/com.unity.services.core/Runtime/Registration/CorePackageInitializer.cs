using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Core.Configuration;
using Unity.Services.Core.Device;
using Unity.Services.Core.Environments;
using UnityEngine;

namespace Unity.Services.Core.Registration
{
    class CorePackageInitializer : IInitializablePackage
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            CoreRegistry.Instance.RegisterPackage(new CorePackageInitializer())
                .ProvidesComponent<IInstallationId>()
                .ProvidesComponent<IEnvironments>()
                .ProvidesComponent<IProjectConfiguration>();
        }

        /// <summary>
        /// This is the Initialize callback that will be triggered by the Core package.
        /// This method will be invoked when the game developer calls UnityServices.Initialize().
        /// </summary>
        /// <param name="registry">
        /// The registry containing components from different packages.
        /// </param>
        /// <returns>
        /// Return a Task representing your initialization.
        /// </returns>
        public async Task Initialize(CoreRegistry registry)
        {
            RegisterInstallationId(registry);
            RegisterEnvironments(registry);
            await RegisterProjectConfigurationAsync(registry);
        }

        internal static void RegisterInstallationId(CoreRegistry registry)
        {
            var installationId = new InstallationId();
            installationId.CreateIdentifier();
            registry.RegisterServiceComponent<IInstallationId>(installationId);
        }

        internal static void RegisterEnvironments(CoreRegistry registry)
        {
            var environments = new Environments.Environments();
            registry.RegisterServiceComponent<IEnvironments>(environments);
        }

        internal static async Task RegisterProjectConfigurationAsync(CoreRegistry registry)
        {
            var options = UnityServices.Instance.Options;
            var projectConfig = await GenerateProjectConfigurationAsync(options);
            registry.RegisterServiceComponent<IProjectConfiguration>(projectConfig);
        }

        internal static async Task<ProjectConfiguration> GenerateProjectConfigurationAsync(
            InitializationOptions options)
        {
            var serializedConfig = await GetSerializedConfigOrEmptyAsync();
            var configValues = new Dictionary<string, ConfigurationEntry>(serializedConfig.Keys.Length);
            configValues.FillWith(serializedConfig);
            configValues.FillWith(options);
            return new ProjectConfiguration(configValues);
        }

        internal static async Task<SerializableProjectConfiguration> GetSerializedConfigOrEmptyAsync()
        {
            try
            {
                var config = await ConfigurationUtils.ConfigurationLoader.GetConfigAsync();
                return config;
            }
            catch (Exception e)
            {
                Debug.LogError(
                    "En error occured while trying to get the project configuration for services." +
                    $"\n{e.Message}" +
                    $"\n{e.StackTrace}");
                return SerializableProjectConfiguration.Empty;
            }
        }
    }
}
