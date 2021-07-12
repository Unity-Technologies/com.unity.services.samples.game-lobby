using System;
using System.Collections.Generic;

namespace Unity.Services.Core.Editor
{
    class ServiceFlags: IServiceFlags
    {
        Dictionary<string, bool> m_FlagsDictionary;
        List<string> m_FlagNames;

        public List<string> GetFlagNames()
        {
            return m_FlagNames;
        }

        public bool DoesFlagExist(string flagName)
        {
            return m_FlagsDictionary.ContainsKey(flagName);
        }

        public bool IsFlagActive(string flagName)
        {
            if (DoesFlagExist(flagName))
            {
                return m_FlagsDictionary[flagName];
            }
            throw new Exception("Flag does not exist");
        }

        public ServiceFlags(Dictionary<string, object> flagsDictionary)
        {
            m_FlagsDictionary = new Dictionary<string, bool>();
            m_FlagNames = new List<string>();
            foreach (var entry in flagsDictionary)
            {
                m_FlagNames.Add(entry.Key);
                m_FlagsDictionary.Add(entry.Key, (bool)entry.Value);
            }
        }
    }
}
