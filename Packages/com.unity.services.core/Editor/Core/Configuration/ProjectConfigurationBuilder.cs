using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Unity.Services.Core.Configuration.Editor
{
    class ProjectConfigurationBuilder : IPreprocessBuildWithReport,
        IPostprocessBuildWithReport,
        IDisposable
    {
        static ProjectConfigurationBuilder s_EditorInstance;

        IEnumerable<IConfigurationProvider> m_OrderedConfigProviders;

        /// <remarks>
        /// Necessary for <see cref="IPreprocessBuildWithReport"/> and
        /// <see cref="IPostprocessBuildWithReport"/> compatibility.
        /// </remarks>
        public ProjectConfigurationBuilder()
            : this(null) {}

        public ProjectConfigurationBuilder(IEnumerable<IConfigurationProvider> orderedConfigProviders)
        {
            m_OrderedConfigProviders = orderedConfigProviders;
        }

        ~ProjectConfigurationBuilder()
        {
            RemoveConfigFromProject();
        }

        [InitializeOnLoadMethod]
        static void CreateEditorInstanceIfNone()
        {
            if (!(s_EditorInstance is null))
            {
                return;
            }

            var orderedConfigProviders = GenerateOrderedConfigurationProviders();
            s_EditorInstance = new ProjectConfigurationBuilder(orderedConfigProviders);
        }

        static IEnumerable<IConfigurationProvider> GenerateOrderedConfigurationProviders()
        {
            return TypeCache.GetTypesDerivedFrom<IConfigurationProvider>()
                .Where(type => !type.IsAbstract)
                .Select(type => (IConfigurationProvider)Activator.CreateInstance(type))
                .OrderBy(prefs => prefs.callbackOrder)
                .ToArray();
        }

        [InitializeOnEnterPlayMode]
        static void SetUpPlayModeConfigOnEnteringPlayMode(EnterPlayModeOptions _)
        {
            CreateEditorInstanceIfNone();
            ConfigurationUtils.ConfigurationLoader = new EditorConfigurationLoader
            {
                PlayModeConfig = s_EditorInstance.BuildConfiguration()
            };
        }

        public SerializableProjectConfiguration BuildConfiguration()
        {
            var builder = new ConfigurationBuilder();
            foreach (var provider in m_OrderedConfigProviders)
            {
                provider.OnBuildingConfiguration(builder);
            }

            return new SerializableProjectConfiguration(builder.Values);
        }

        public void GenerateConfigFileInProject()
        {
            var config = BuildConfiguration();
            var serializedConfig = EditorJsonUtility.ToJson(config);
            AddConfigToProject(serializedConfig);
        }

        public static void AddConfigToProject(string config)
        {
            if (!AssetDatabase.IsValidFolder(ConfigurationUtils.StreamingAssetsPath))
            {
                AssetDatabase.CreateFolder("Assets", ConfigurationUtils.StreamingAssetsFolder);
            }

            File.WriteAllText(ConfigurationUtils.RuntimeConfigFullPath, config);
            AssetDatabase.Refresh();
        }

        public static void RemoveConfigFromProject()
        {
            AssetDatabase.DeleteAsset(ConfigurationUtils.ConfigAssetPath);
        }

        int IOrderedCallback.callbackOrder { get; }

        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
        {
            if (m_OrderedConfigProviders is null)
            {
                m_OrderedConfigProviders = GenerateOrderedConfigurationProviders();
            }

            GenerateConfigFileInProject();
        }

        void IPostprocessBuildWithReport.OnPostprocessBuild(BuildReport report)
        {
            RemoveConfigFromProject();
        }

        void IDisposable.Dispose()
        {
            RemoveConfigFromProject();
        }
    }
}
