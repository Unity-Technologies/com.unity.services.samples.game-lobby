using System;
using UnityEngine;

namespace Unity.Services.Authentication.Utilities
{
    interface ICache
    {
        bool HasKey(string key);
        void DeleteKey(string key);

        void SetString(string key, string value);
        string GetString(string key);
    }

    class PlayerPrefsCache : ICache
    {
        readonly string m_Prefix;

        public PlayerPrefsCache(string prefix)
        {
            m_Prefix = prefix + ".";
        }

        public bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(m_Prefix + key);
        }

        public void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(m_Prefix + key);
        }

        public string GetString(string key)
        {
            return PlayerPrefs.GetString(m_Prefix + key);
        }

        public void SetString(string key, string value)
        {
            PlayerPrefs.SetString(m_Prefix + key, value);
            PlayerPrefs.Save();
        }
    }
}
