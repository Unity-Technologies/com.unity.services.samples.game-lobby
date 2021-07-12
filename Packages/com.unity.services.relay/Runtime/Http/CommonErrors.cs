namespace Unity.Services.Relay.Http
{
    static internal class CommonErrors
    {
        private const string ErrorPrefix = "com.unity.services.relay";

        public static IError CreateUnspecifiedHttpError(string details)
        {
            return new BasicError(
                $"{ErrorPrefix}http.httperror",
                "Unspecified HTTP error",
                null,
                0,
                details
            );
        }

        public static IError RequestOnSuccessNull => new BasicError(
            $"{ErrorPrefix}onsuccessnullerror",
            "Request must have an onSuccess callback",
            null,
            0, //TODO: define a code space for SDK side errors so all errors can be identified by a code
            "");


        public static IError HttpNetworkError => new BasicError(
            $"{ErrorPrefix}httpclient.networkerror",
            "Network Error",
            null,
            0,
            ""
        );
        
    }
}