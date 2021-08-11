using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Core.Configuration;
using Unity.Services.Core.Configuration.Internal;
using Unity.Services.Core.Device;
using Unity.Services.Core.Device.Internal;
using Unity.Services.Core.Environments;
using Unity.Services.Core.Environments.Internal;
using Unity.Services.Core.Internal;
using UnityEngine;
using NotNull = JetBrains.Annotations.NotNullAttribute;

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
            // There are potential race conditions with other services we're trying to avoid by calling
            // RegisterInstallationId as the _very first_ thing we do.
            RegisterInstallationId(registry);

            var projectConfiguration = await RegisterProjectConfigurationAsync(
                registry, UnityServices.Instance.Options);
            RegisterEnvironments(registry, projectConfiguration);
        }

        internal static void RegisterInstallationId(CoreRegistry registry)
        {
            var installationId = new InstallationId();
            installationId.CreateIdentifier();
            registry.RegisterServiceComponent<IInstallationId>(installationId);
        }

        internal static void RegisterEnvironments(CoreRegistry registry, IProjectConfiguration projectConfiguration)
        {
            var environments = new Environments.Internal.Environments();
            environments.Current = projectConfiguration.GetString(EnvironmentsOptionsExtensions.EnvironmentNameKey, "production");
            registry.RegisterServiceComponent<IEnvironments>(environments);
        }

        internal static async Task<IProjectConfiguration> RegisterProjectConfigurationAsync(
            [NotNull] CoreRegistry registry,
            [NotNull] InitializationOptions options)
        {
            var projectConfig = await GenerateProjectConfigurationAsync(options);
            registry.RegisterServiceComponent<IProjectConfiguration>(projectConfig);
            return projectConfig;
        }

        internal static async Task<ProjectConfiguration> GenerateProjectConfigurationAsync(
            [NotNull] InitializationOptions options)
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
                CoreLogger.LogError(
                    "En error occured while trying to get the project configuration for services." +
                    $"\n{e.Message}" +
                    $"\n{e.StackTrace}");
                return SerializableProjectConfiguration.Empty;
            }
        }
    }
}
