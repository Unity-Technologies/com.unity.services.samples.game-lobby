using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Unity.Services.Relay.Http;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Services.Relay.Models;
using Unity.Services.Authentication.Internal;

namespace Unity.Services.Relay.Allocations
{
    internal static class JsonSerialization
    {
        public static byte[] Serialize<T>(T obj)
        {
            return Encoding.UTF8.GetBytes(SerializeToString(obj));
        }

        public static string SerializeToString<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
    }
    [Preserve]
    public class AllocationsApiBaseRequest
    {
        [Preserve]
        public List<string> AddParamsToQueryParams(List<string> queryParams, string key, string value)
        {
            key = UnityWebRequest.EscapeURL(key);
            value = UnityWebRequest.EscapeURL(value);
            queryParams.Add($"{key}={value}");

            return queryParams;
        }

        [Preserve]
        public List<string> AddParamsToQueryParams(List<string> queryParams, string key, List<string> values, string style, bool explode)
        {
            if (explode)
            {
                foreach(var value in values)
                {
                    string escapedValue = UnityWebRequest.EscapeURL(value);
                    queryParams.Add($"{UnityWebRequest.EscapeURL(key)}={escapedValue}");
                }
            }
            else
            {
                string paramString = $"{UnityWebRequest.EscapeURL(key)}=";
                foreach(var value in values)
                {
                    paramString += UnityWebRequest.EscapeURL(value) + ",";
                }
                paramString = paramString.Remove(paramString.Length - 1);
                queryParams.Add(paramString);
            }

            return queryParams;
        }

        [Preserve]
        public List<string> AddParamsToQueryParams<T>(List<string> queryParams, string key, T value)
        {
            if (queryParams == null)
            {
                queryParams = new List<string>();
            }

            key = UnityWebRequest.EscapeURL(key);
            string valueString = UnityWebRequest.EscapeURL(value.ToString());
            queryParams.Add($"{key}={valueString}");
            return queryParams;
        }

        public byte[] ConstructBody(System.IO.Stream stream)
        {
            if (stream != null)
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                {
                    stream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
            return null;
        }

        public byte[] ConstructBody(string s)
        {
            return System.Text.Encoding.UTF8.GetBytes(s);
        }

        public byte[] ConstructBody(object o)
        {
            return JsonSerialization.Serialize(o);
        }

        public string GenerateAcceptHeader(string[] accepts)
        {
            if (accepts.Length == 0)
            {
                return null;
            }
            for (int i = 0; i < accepts.Length; ++i)
            {
                if (string.Equals(accepts[i], "application/json", System.StringComparison.OrdinalIgnoreCase))
                {
                    return "application/json";
                }
            }
            return string.Join(", ", accepts);
        }

        private static readonly Regex JsonRegex = new Regex(@"application\/json(;\s)?((charset=utf8|q=[0-1]\.\d)(\s)?)*");

        public string GenerateContentTypeHeader(string[] contentTypes)
        {
            if (contentTypes.Length == 0)
            {
                return null;
            }

            for(int i = 0; i < contentTypes.Length; ++i)
            {
                if (!string.IsNullOrWhiteSpace(contentTypes[i]) && JsonRegex.IsMatch(contentTypes[i]))
                {
                    return contentTypes[i];
                }
            }
            return contentTypes[0];
        }

        public IMultipartFormSection GenerateMultipartFormFileSection(string paramName, System.IO.Stream stream, string contentType)
        {
            if (stream is System.IO.FileStream)
            {
                System.IO.FileStream fileStream = (System.IO.FileStream) stream;
                return new MultipartFormFileSection(paramName, ConstructBody(fileStream), GetFileName(fileStream.Name), contentType);
            }
            return new MultipartFormDataSection(paramName, ConstructBody(stream));
        }

        private string GetFileName(string filePath)
        {
            return System.IO.Path.GetFileName(filePath);
        }
    }

    [Preserve]
    public class CreateAllocationRequest : AllocationsApiBaseRequest
    {
        [Preserve]
        public AllocationRequest AllocationRequest { get; }
        string PathAndQueryParams;

        /// <summary>
        /// CreateAllocation Request Object.
        /// Create Allocation
        /// </summary>
        /// <param name="AllocationRequest">AllocationRequest param</param>
        /// <returns>A CreateAllocation request object.</returns>
        [Preserve]
        public CreateAllocationRequest(AllocationRequest allocationRequest)
        {
            AllocationRequest = allocationRequest;
            PathAndQueryParams = $"/v1/allocate";

            List<string> queryParams = new List<string>();

            if (queryParams.Count > 0)
            {
                PathAndQueryParams = $"{PathAndQueryParams}?{string.Join("&", queryParams)}";
            }
        }

        public string ConstructUrl(string requestBasePath)
        {
            return requestBasePath + PathAndQueryParams;
        }

        public byte[] ConstructBody()
        {
            return ConstructBody(AllocationRequest);
        }

        public Dictionary<string, string> ConstructHeaders(IAccessToken accessToken,
            Configuration operationConfiguration = null)
        {
            var headers = new Dictionary<string, string>();
            if(!string.IsNullOrEmpty(accessToken.AccessToken))
            {
                headers.Add("authorization", "Bearer " + accessToken.AccessToken);
            }

            string[] contentTypes = {
                "application/json"
            };

            string[] accepts = {
                "application/json",
                "application/problem+json"
            };

            var acceptHeader = GenerateAcceptHeader(accepts);
            if (!string.IsNullOrEmpty(acceptHeader))
            {
                headers.Add("Accept", acceptHeader);
            }
            var contentTypeHeader = GenerateContentTypeHeader(contentTypes);
            if (!string.IsNullOrEmpty(contentTypeHeader))
            {
                headers.Add("Content-Type", contentTypeHeader);
            }


            // We also check if there are headers that are defined as part of
            // the request configuration.
            if (operationConfiguration != null && operationConfiguration.Headers != null)
            {
                foreach (var pair in operationConfiguration.Headers)
                {
                    headers[pair.Key] = pair.Value;
                }
            }

            return headers;
        }
    }
    [Preserve]
    public class CreateJoincodeRequest : AllocationsApiBaseRequest
    {
        [Preserve]
        public JoinCodeRequest JoinCodeRequest { get; }
        string PathAndQueryParams;

        /// <summary>
        /// CreateJoincode Request Object.
        /// Create Join Code
        /// </summary>
        /// <param name="JoinCodeRequest">JoinCodeRequest param</param>
        /// <returns>A CreateJoincode request object.</returns>
        [Preserve]
        public CreateJoincodeRequest(JoinCodeRequest joinCodeRequest)
        {
            JoinCodeRequest = joinCodeRequest;
            PathAndQueryParams = $"/v1/joincode";

            List<string> queryParams = new List<string>();

            if (queryParams.Count > 0)
            {
                PathAndQueryParams = $"{PathAndQueryParams}?{string.Join("&", queryParams)}";
            }
        }

        public string ConstructUrl(string requestBasePath)
        {
            return requestBasePath + PathAndQueryParams;
        }

        public byte[] ConstructBody()
        {
            return ConstructBody(JoinCodeRequest);
        }

        public Dictionary<string, string> ConstructHeaders(IAccessToken accessToken,
            Configuration operationConfiguration = null)
        {
            var headers = new Dictionary<string, string>();
            if(!string.IsNullOrEmpty(accessToken.AccessToken))
            {
                headers.Add("authorization", "Bearer " + accessToken.AccessToken);
            }

            string[] contentTypes = {
                "application/json"
            };

            string[] accepts = {
                "application/json",
                "application/problem+json"
            };

            var acceptHeader = GenerateAcceptHeader(accepts);
            if (!string.IsNullOrEmpty(acceptHeader))
            {
                headers.Add("Accept", acceptHeader);
            }
            var contentTypeHeader = GenerateContentTypeHeader(contentTypes);
            if (!string.IsNullOrEmpty(contentTypeHeader))
            {
                headers.Add("Content-Type", contentTypeHeader);
            }


            // We also check if there are headers that are defined as part of
            // the request configuration.
            if (operationConfiguration != null && operationConfiguration.Headers != null)
            {
                foreach (var pair in operationConfiguration.Headers)
                {
                    headers[pair.Key] = pair.Value;
                }
            }

            return headers;
        }
    }
    [Preserve]
    public class JoinRelayRequest : AllocationsApiBaseRequest
    {
        [Preserve]
        public JoinRequest JoinRequest { get; }
        string PathAndQueryParams;

        /// <summary>
        /// JoinRelay Request Object.
        /// Join Relay
        /// </summary>
        /// <param name="JoinRequest">JoinRequest param</param>
        /// <returns>A JoinRelay request object.</returns>
        [Preserve]
        public JoinRelayRequest(JoinRequest joinRequest)
        {
            JoinRequest = joinRequest;
            PathAndQueryParams = $"/v1/join";

            List<string> queryParams = new List<string>();

            if (queryParams.Count > 0)
            {
                PathAndQueryParams = $"{PathAndQueryParams}?{string.Join("&", queryParams)}";
            }
        }

        public string ConstructUrl(string requestBasePath)
        {
            return requestBasePath + PathAndQueryParams;
        }

        public byte[] ConstructBody()
        {
            return ConstructBody(JoinRequest);
        }

        public Dictionary<string, string> ConstructHeaders(IAccessToken accessToken,
            Configuration operationConfiguration = null)
        {
            var headers = new Dictionary<string, string>();
            if(!string.IsNullOrEmpty(accessToken.AccessToken))
            {
                headers.Add("authorization", "Bearer " + accessToken.AccessToken);
            }

            string[] contentTypes = {
                "application/json"
            };

            string[] accepts = {
                "application/json",
                "application/problem+json"
            };

            var acceptHeader = GenerateAcceptHeader(accepts);
            if (!string.IsNullOrEmpty(acceptHeader))
            {
                headers.Add("Accept", acceptHeader);
            }
            var contentTypeHeader = GenerateContentTypeHeader(contentTypes);
            if (!string.IsNullOrEmpty(contentTypeHeader))
            {
                headers.Add("Content-Type", contentTypeHeader);
            }


            // We also check if there are headers that are defined as part of
            // the request configuration.
            if (operationConfiguration != null && operationConfiguration.Headers != null)
            {
                foreach (var pair in operationConfiguration.Headers)
                {
                    headers[pair.Key] = pair.Value;
                }
            }

            return headers;
        }
    }
    [Preserve]
    public class ListRegionsRequest : AllocationsApiBaseRequest
    {
        string PathAndQueryParams;

        /// <summary>
        /// ListRegions Request Object.
        /// List relay regions
        /// </summary>
        /// <returns>A ListRegions request object.</returns>
        [Preserve]
        public ListRegionsRequest()
        {
            PathAndQueryParams = $"/v1/regions";

            List<string> queryParams = new List<string>();

            if (queryParams.Count > 0)
            {
                PathAndQueryParams = $"{PathAndQueryParams}?{string.Join("&", queryParams)}";
            }
        }

        public string ConstructUrl(string requestBasePath)
        {
            return requestBasePath + PathAndQueryParams;
        }

        public byte[] ConstructBody()
        {
            return null;
        }

        public Dictionary<string, string> ConstructHeaders(IAccessToken accessToken,
            Configuration operationConfiguration = null)
        {
            var headers = new Dictionary<string, string>();
            if(!string.IsNullOrEmpty(accessToken.AccessToken))
            {
                headers.Add("authorization", "Bearer " + accessToken.AccessToken);
            }

            string[] contentTypes = {
            };

            string[] accepts = {
                "application/json",
                "application/problem+json"
            };

            var acceptHeader = GenerateAcceptHeader(accepts);
            if (!string.IsNullOrEmpty(acceptHeader))
            {
                headers.Add("Accept", acceptHeader);
            }
            var contentTypeHeader = GenerateContentTypeHeader(contentTypes);
            if (!string.IsNullOrEmpty(contentTypeHeader))
            {
                headers.Add("Content-Type", contentTypeHeader);
            }


            // We also check if there are headers that are defined as part of
            // the request configuration.
            if (operationConfiguration != null && operationConfiguration.Headers != null)
            {
                foreach (var pair in operationConfiguration.Headers)
                {
                    headers[pair.Key] = pair.Value;
                }
            }

            return headers;
        }
    }
}
