using Newtonsoft.Json;

namespace Unity.Services.Rooms.Http
{
    internal static class JsonHelpers
    {
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
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            result = JsonConvert.DeserializeObject<T>(@this, settings);
            return success;
        }
    }
}
