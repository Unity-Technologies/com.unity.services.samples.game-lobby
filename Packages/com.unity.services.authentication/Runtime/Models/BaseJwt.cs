using System;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Unity.Services.Authentication.Models
{
    class BaseJwt
    {
        [Preserve]
        public BaseJwt() {}

        [JsonProperty("exp")]
        public int ExpirationTimeUnix;
        [JsonProperty("iat")]
        public int IssuedAtTimeUnix;
        [JsonProperty("nbf")]
        public int NotBeforeTimeUnix;

        [JsonIgnore]
        public DateTime ExpirationTime => ConvertTimestamp(ExpirationTimeUnix);

        [JsonIgnore]
        public DateTime IssuedAtTime => ConvertTimestamp(IssuedAtTimeUnix);

        [JsonIgnore]
        public DateTime NotBeforeTime => ConvertTimestamp(NotBeforeTimeUnix);

        protected DateTime ConvertTimestamp(int timestamp)
        {
            if (timestamp != 0)
            {
                var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(timestamp);
                return dateTimeOffset.DateTime;
            }

            throw new Exception("Token does not contain a value for this timestamp.");
        }
    }
}
