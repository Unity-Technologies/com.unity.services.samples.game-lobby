using System;
using Unity.Services.Core.Device.Internal;
using UnityEngine;

namespace Unity.Services.Core.Device
{
    class InstallationId : IInstallationId
    {
        const string k_UnityInstallationIdKey = "UnityInstallationId";

        internal string identifier;

        internal IUserIdentifierProvider unityAdsIdentifierProvider;
        internal IUserIdentifierProvider unityAnalyticsIdentifierProvider;

        public InstallationId()
        {
            unityAdsIdentifierProvider = new UnityAdsIdentifier();
            unityAnalyticsIdentifierProvider = new UnityAnalyticsIdentifier();
        }

        public string GetOrCreateIdentifier()
        {
            if (string.IsNullOrEmpty(identifier))
                CreateIdentifier();

            return identifier;
        }

        public void CreateIdentifier()
        {
            identifier = ReadIdentifierFromFile();
            if (!string.IsNullOrEmpty(identifier))
                return;

            var analyticsId = unityAnalyticsIdentifierProvider.UserId;
            var adsId = unityAdsIdentifierProvider.UserId;

            if (!string.IsNullOrEmpty(analyticsId))
            {
                identifier = analyticsId;
            }
            else if (!string.IsNullOrEmpty(adsId))
            {
                identifier = adsId;
            }
            else
            {
                identifier = GenerateGuid();
            }

            WriteIdentifierToFile(identifier);

            if (string.IsNullOrEmpty(analyticsId))
            {
                unityAnalyticsIdentifierProvider.UserId = identifier;
            }

            if (string.IsNullOrEmpty(adsId))
            {
                unityAdsIdentifierProvider.UserId = identifier;
            }
        }

        string ReadIdentifierFromFile()
        {
            return PlayerPrefs.GetString(k_UnityInstallationIdKey);
        }

        void WriteIdentifierToFile(string identifier)
        {
            PlayerPrefs.SetString(k_UnityInstallationIdKey, identifier);
            PlayerPrefs.Save();
        }

        string GenerateGuid()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
