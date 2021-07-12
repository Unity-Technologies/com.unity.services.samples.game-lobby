using System;
using Newtonsoft.Json;

namespace Unity.Services.Core.Editor
{
    static class JsonHelper
    {
        internal static bool TryJsonDeserialize<T>(string json, ref T dest)
        {
            if (!string.IsNullOrEmpty((json)))
            {
                try
                {
                    dest = JsonConvert.DeserializeObject<T>(json);
                    return true;
                }
                catch (Exception)
                {
                    // ignored, JSON parsing failed, we'll return null
                }
            }

            return false;
        }
    }
}
