using System;
using System.Collections.Generic;
using System.Text;

namespace Unity.Services.Authentication.Utilities
{
    static class HttpUtilities
    {
        /// <summary>
        /// Parse and decode the query string to a dictionary with values decoded.
        /// </summary>
        /// <remarks>
        /// This function only works when there is a single value per key.
        /// </remarks>
        /// <param name="queryString">The query string to parse.</param>
        /// <returns>The query string decoded.</returns>
        public static IDictionary<string, string> ParseQueryString(string queryString)
        {
            var result = new Dictionary<string, string>();
            var splitQuery = queryString.Split('?', '&');

            foreach (var param in splitQuery)
            {
                var assignmentIndex = param.IndexOf('=');
                if (assignmentIndex >= 0)
                {
                    var paramName = UnescapeUrlString(param.Substring(0, assignmentIndex));
                    var paramValue = UnescapeUrlString(param.Substring(assignmentIndex + 1));
                    result[paramName] = paramValue;
                }
            }

            return result;
        }

        /// <summary>
        /// Encode the dictionary to a URL query parameter.
        /// </summary>
        /// <remarks>
        /// This function only works when there is a single value per key.
        /// </remarks>
        /// <param name="queryParams">A dictionary that represents the query parameters.</param>
        /// <returns>The encoded query string without the preceding question mark.</returns>
        public static string EncodeQueryString(IDictionary<string, string> queryParams)
        {
            var result = new StringBuilder();

            var firstParam = true;
            foreach (var param in queryParams)
            {
                if (!firstParam)
                {
                    result.Append('&');
                }
                else
                {
                    firstParam = false;
                }

                result.Append(EscapeUrlString(param.Key)).Append('=').Append(EscapeUrlString(param.Value));
            }

            return result.ToString();
        }

        /// <summary>
        /// Encode any raw string to a URL encoded string that can be placed after =.
        /// </summary>
        /// <param name="rawString">The string to encode. Any special character is okay.</param>
        /// <returns>The URL encoded string.</returns>
        static string EscapeUrlString(string rawString)
        {
            // Don't use Uri.EscapeUriString it has issue encoding reserved characters.
            return Uri.EscapeDataString(rawString);
        }

        /// <summary>
        /// Decode the URL escaped string to a raw URL.
        /// </summary>
        /// <param name="urlString">The url string to decode.</param>
        /// <returns>The raw string decoded.</returns>
        static string UnescapeUrlString(string urlString)
        {
            return Uri.UnescapeDataString(urlString);
        }
    }
}
