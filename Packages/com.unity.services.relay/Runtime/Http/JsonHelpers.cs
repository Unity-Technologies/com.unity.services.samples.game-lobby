using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Utilities;
using UnityEngine;

namespace Unity.Services.Relay.Http
{
    internal static class JsonHelpers
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        internal static void RegisterTypesForAOT()
        {
            AotHelper.EnsureType<StringEnumConverter>();
        }

        internal static bool TryParseJson<T>(this string @this, out T result)
        {
            var success = true;
            var settings = new JsonSerializerSettings
            {
                Error = (sender, args) =>
                {
                    success = false;
                    args.ErrorContext.Handled = true;
                },
                MissingMemberHandling = Newtonsoft.Json.MissingMemberHandling.Ignore
            };
            result = JsonConvert.DeserializeObject<T>(@this, settings);
            return success;
        }
    }
}