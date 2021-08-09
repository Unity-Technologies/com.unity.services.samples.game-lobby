using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Unity.Services.Lobbies.Http;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;

namespace Unity.Services.Lobbies.Lobby
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
    public class LobbyApiBaseRequest
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
    public class CreateLobbyRequest : LobbyApiBaseRequest
    {
        [Preserve]
        public CreateRequest CreateRequest { get; }
        string PathAndQueryParams;

        /// <summary>
        /// CreateLobby Request Object.
        /// Create a lobby
        /// </summary>
        /// <param name="CreateRequest">CreateRequest param</param>
        /// <returns>A CreateLobby request object.</returns>
        [Preserve]
        public CreateLobbyRequest(CreateRequest createRequest = default(CreateRequest))
        {
            CreateRequest = createRequest;
            PathAndQueryParams = $"/create";

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
            if(CreateRequest != null)
            {
                return ConstructBody(CreateRequest);
            }
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
    public class DeleteLobbyRequest : LobbyApiBaseRequest
    {
        [Preserve]
        public string LobbyId { get; }
        string PathAndQueryParams;

        /// <summary>
        /// DeleteLobby Request Object.
        /// Delete a lobby
        /// </summary>
        /// <param name="lobbyId">The id of the lobby to execute the request against.</param>
        /// <returns>A DeleteLobby request object.</returns>
        [Preserve]
        public DeleteLobbyRequest(string lobbyId)
        {
            LobbyId = lobbyId;
            PathAndQueryParams = $"/{lobbyId}";

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
    public class GetLobbyRequest : LobbyApiBaseRequest
    {
        [Preserve]
        public string LobbyId { get; }
        string PathAndQueryParams;

        /// <summary>
        /// GetLobby Request Object.
        /// Get lobby details
        /// </summary>
        /// <param name="lobbyId">The id of the lobby to execute the request against.</param>
        /// <returns>A GetLobby request object.</returns>
        [Preserve]
        public GetLobbyRequest(string lobbyId)
        {
            LobbyId = lobbyId;
            PathAndQueryParams = $"/{lobbyId}";

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
    [Preserve]
    public class HeartbeatRequest : LobbyApiBaseRequest
    {
        [Preserve]
        public string LobbyId { get; }
        [Preserve]
        public object Body { get; }
        string PathAndQueryParams;

        /// <summary>
        /// Heartbeat Request Object.
        /// Heartbeat a lobby
        /// </summary>
        /// <param name="lobbyId">The id of the lobby to execute the request against.</param>
        /// <param name="body">body param</param>
        /// <returns>A Heartbeat request object.</returns>
        [Preserve]
        public HeartbeatRequest(string lobbyId, object body = default(object))
        {
            LobbyId = lobbyId;
            Body = body;
            PathAndQueryParams = $"/{lobbyId}/heartbeat";

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
            if(Body != null)
            {
                return ConstructBody(Body);
            }
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
                "application/json"
            };

            string[] accepts = {
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
    public class JoinLobbyByCodeRequest : LobbyApiBaseRequest
    {
        [Preserve]
        public JoinByCodeRequest JoinByCodeRequest { get; }
        string PathAndQueryParams;

        /// <summary>
        /// JoinLobbyByCode Request Object.
        /// Join a lobby with lobby code
        /// </summary>
        /// <param name="JoinByCodeRequest">JoinByCodeRequest param</param>
        /// <returns>A JoinLobbyByCode request object.</returns>
        [Preserve]
        public JoinLobbyByCodeRequest(JoinByCodeRequest joinByCodeRequest = default(JoinByCodeRequest))
        {
            JoinByCodeRequest = joinByCodeRequest;
            PathAndQueryParams = $"/joinbycode";

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
            if(JoinByCodeRequest != null)
            {
                return ConstructBody(JoinByCodeRequest);
            }
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
    public class JoinLobbyByIdRequest : LobbyApiBaseRequest
    {
        [Preserve]
        public string LobbyId { get; }
        [Preserve]
        public Player Player { get; }
        string PathAndQueryParams;

        /// <summary>
        /// JoinLobbyById Request Object.
        /// Join a lobby with lobby ID
        /// </summary>
        /// <param name="lobbyId">The id of the lobby to execute the request against.</param>
        /// <param name="Player">Player param</param>
        /// <returns>A JoinLobbyById request object.</returns>
        [Preserve]
        public JoinLobbyByIdRequest(string lobbyId, Player player = default(Player))
        {
            LobbyId = lobbyId;
            Player = player;
            PathAndQueryParams = $"/{lobbyId}/join";

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
            if(Player != null)
            {
                return ConstructBody(Player);
            }
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
    public class QueryLobbiesRequest : LobbyApiBaseRequest
    {
        [Preserve]
        public QueryRequest QueryRequest { get; }
        string PathAndQueryParams;

        /// <summary>
        /// QueryLobbies Request Object.
        /// Query public lobbies
        /// </summary>
        /// <param name="QueryRequest">QueryRequest param</param>
        /// <returns>A QueryLobbies request object.</returns>
        [Preserve]
        public QueryLobbiesRequest(QueryRequest queryRequest = default(QueryRequest))
        {
            QueryRequest = queryRequest;
            PathAndQueryParams = $"/query";

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
            if(QueryRequest != null)
            {
                return ConstructBody(QueryRequest);
            }
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
    public class QuickJoinLobbyRequest : LobbyApiBaseRequest
    {
        [Preserve]
        public QuickJoinRequest QuickJoinRequest { get; }
        string PathAndQueryParams;

        /// <summary>
        /// QuickJoinLobby Request Object.
        /// Query available lobbies and join a random one
        /// </summary>
        /// <param name="QuickJoinRequest">QuickJoinRequest param</param>
        /// <returns>A QuickJoinLobby request object.</returns>
        [Preserve]
        public QuickJoinLobbyRequest(QuickJoinRequest quickJoinRequest = default(QuickJoinRequest))
        {
            QuickJoinRequest = quickJoinRequest;
            PathAndQueryParams = $"/quickjoin";

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
            if(QuickJoinRequest != null)
            {
                return ConstructBody(QuickJoinRequest);
            }
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
    public class RemovePlayerRequest : LobbyApiBaseRequest
    {
        [Preserve]
        public string LobbyId { get; }
        [Preserve]
        public string PlayerId { get; }
        string PathAndQueryParams;

        /// <summary>
        /// RemovePlayer Request Object.
        /// Remove a player
        /// </summary>
        /// <param name="lobbyId">The id of the lobby to execute the request against.</param>
        /// <param name="playerId">The id of the player to execute the request against.</param>
        /// <returns>A RemovePlayer request object.</returns>
        [Preserve]
        public RemovePlayerRequest(string lobbyId, string playerId)
        {
            LobbyId = lobbyId;
            PlayerId = playerId;
            PathAndQueryParams = $"/{lobbyId}/players/{playerId}";

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
    public class UpdateLobbyRequest : LobbyApiBaseRequest
    {
        [Preserve]
        public string LobbyId { get; }
        [Preserve]
        public UpdateRequest UpdateRequest { get; }
        string PathAndQueryParams;

        /// <summary>
        /// UpdateLobby Request Object.
        /// Update lobby data
        /// </summary>
        /// <param name="lobbyId">The id of the lobby to execute the request against.</param>
        /// <param name="UpdateRequest">UpdateRequest param</param>
        /// <returns>A UpdateLobby request object.</returns>
        [Preserve]
        public UpdateLobbyRequest(string lobbyId, UpdateRequest updateRequest = default(UpdateRequest))
        {
            LobbyId = lobbyId;
            UpdateRequest = updateRequest;
            PathAndQueryParams = $"/{lobbyId}";

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
            if(UpdateRequest != null)
            {
                return ConstructBody(UpdateRequest);
            }
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
    public class UpdatePlayerRequest : LobbyApiBaseRequest
    {
        [Preserve]
        public string LobbyId { get; }
        [Preserve]
        public string PlayerId { get; }
        [Preserve]
        public PlayerUpdateRequest PlayerUpdateRequest { get; }
        string PathAndQueryParams;

        /// <summary>
        /// UpdatePlayer Request Object.
        /// Update player data
        /// </summary>
        /// <param name="lobbyId">The id of the lobby to execute the request against.</param>
        /// <param name="playerId">The id of the player to execute the request against.</param>
        /// <param name="PlayerUpdateRequest">PlayerUpdateRequest param</param>
        /// <returns>A UpdatePlayer request object.</returns>
        [Preserve]
        public UpdatePlayerRequest(string lobbyId, string playerId, PlayerUpdateRequest playerUpdateRequest = default(PlayerUpdateRequest))
        {
            LobbyId = lobbyId;
            PlayerId = playerId;
            PlayerUpdateRequest = playerUpdateRequest;
            PathAndQueryParams = $"/{lobbyId}/players/{playerId}";

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
            if(PlayerUpdateRequest != null)
            {
                return ConstructBody(PlayerUpdateRequest);
            }
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
}
