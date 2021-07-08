using System;

namespace Unity.Services.Authentication.Editor
{
    static class AuthenticationSettingsHelper
    {
        internal static Exception ExtractException(Exception exception)
        {
            var aggregatedException = exception as AggregateException;
            if (aggregatedException == null)
            {
                return exception;
            }

            if (aggregatedException.InnerExceptions.Count > 1)
            {
                // There are multiple exceptions aggregated, don't try to extract exception.
                return exception;
            }

            // It returns the first exception.
            return aggregatedException.InnerException;
        }

        internal static string ExceptionToString(Exception exception)
        {
            var errorMessage = "[ERROR] ";
            var currentError = exception;
            var firstError = true;
            while (currentError != null)
            {
                if (!firstError)
                {
                    errorMessage += "\n---> ";
                }
                else
                {
                    firstError = false;
                }
                errorMessage += currentError.Message;
                currentError = currentError.InnerException;
            }

            return errorMessage;
        }
    }
}
