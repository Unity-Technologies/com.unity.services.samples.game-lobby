using System;

namespace Unity.Services.Authentication.Utilities
{
    interface IDateTimeWrapper
    {
        double SecondsSinceUnixEpoch();

        DateTime UtcNow { get; }
    }

    class DateTimeWrapper : IDateTimeWrapper
    {
        static readonly DateTime k_UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        public double SecondsSinceUnixEpoch()
        {
            return Math.Round((DateTime.UtcNow - k_UnixEpoch).TotalSeconds);
        }

        public DateTime UtcNow => DateTime.UtcNow;
    }
}
