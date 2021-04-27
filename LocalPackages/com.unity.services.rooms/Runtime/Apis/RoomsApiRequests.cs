using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Services.Rooms.Models;

namespace Unity.Services.Rooms.Rooms
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
    public class RoomsApiBaseRequest
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
        public List<string> AddParamsToQueryParams(List<string> queryParams, string key, List<string> values)
        {
            foreach(var value in values)
            {
                string escapedValue = UnityWebRequest.EscapeURL(value);
                queryParams.Add($"{UnityWebRequest.EscapeURL(key)}[]={escapedValue}");
            }
            return queryParams;
        }

        [Preserve]
        public List<string> AddParamsToQueryParams<T>(List<string> queryParams, string key, T value)
        {
            key = UnityWebRequest.EscapeURL(key);
            string valueString = UnityWebRequest.EscapeURL(value.ToString());
            queryParams.Add($"{key}={valueString}");
            return queryParams;
        }

        public string GenerateAcceptHeader(String[] accepts)
        {
            if (accepts.Length == 0)
            {
                return null;
            }
            for (int i = 0; i < accepts.Length; ++i)
            {
                if (String.Equals(accepts[i], "application/json", StringComparison.OrdinalIgnoreCase))
                {
                    return "application/json";
                }
            }
            return String.Join(", ", accepts);
        }

        private static readonly Regex JsonRegex = new Regex(@"application\/json(;\s)?((charset=utf8|q=[0-1]\.\d)(\s)?)*");

        public string GenerateContentTypeHeader(String[] contentTypes)
        {
            if (contentTypes.Length == 0)
            {
                return null;
            }

            for(int i = 0; i < contentTypes.Length; ++i)
            {
                if (!String.IsNullOrWhiteSpace(contentTypes[i]) && JsonRegex.IsMatch(contentTypes[i]))
                {
                    return contentTypes[i];
                }
            }
            return contentTypes[0];
        }
    }

    [Preserve]
    public class CreateRoomRequest : RoomsApiBaseRequest
    {
        [Preserve]
        public string Upid { get; }
        [Preserve]
        public string Uasid { get; }
        [Preserve]
        public CreateRequest Body { get; }
        string PathAndQueryParams;

        [Preserve]
        public CreateRoomRequest(string upid = default(string), string uasid = default(string), CreateRequest body = default(CreateRequest))
        {
            Upid = upid;
            Uasid = uasid;
            Body = body;
            PathAndQueryParams = $"/create";

            List<string> queryParams = new List<string>();

            if (queryParams.Count > 0)
            {
                PathAndQueryParams = $"{PathAndQueryParams}?{string.Join("&", queryParams)}";
            }
        }

        public string ConstructUrl()
        {
            return Unity.Services.Rooms.Configuration.BasePath + PathAndQueryParams;
        }

        public byte[] ConstructBody()
        {
            return JsonSerialization.Serialize(Body);
        }

        public Dictionary<string, string> ConstructHeaders()
        {
            var headers = new Dictionary<string, string>();

            String[] contentTypes = {
                "application/json"
            };

            String[] accepts = {
                "application/json"
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

            if(!string.IsNullOrEmpty(Upid))
            {
                headers.Add("Upid", Upid);
            }
            if(!string.IsNullOrEmpty(Uasid))
            {
                headers.Add("Uasid", Uasid);
            }

            return headers;
        }
    }

    [Preserve]
    public class DeleteRoomRequest : RoomsApiBaseRequest
    {
        [Preserve]
        public string Upid { get; }
        [Preserve]
        public string Uasid { get; }
        [Preserve]
        public string RoomId { get; }
        string PathAndQueryParams;

        [Preserve]
        public DeleteRoomRequest(string upid = default(string), string uasid = default(string), string roomId = default(string))
        {
            Upid = upid;
            Uasid = uasid;
            RoomId = roomId;
            PathAndQueryParams = $"/{roomId}";

            List<string> queryParams = new List<string>();

            if (queryParams.Count > 0)
            {
                PathAndQueryParams = $"{PathAndQueryParams}?{string.Join("&", queryParams)}";
            }
        }

        public string ConstructUrl()
        {
            return Unity.Services.Rooms.Configuration.BasePath + PathAndQueryParams;
        }

        public byte[] ConstructBody()
        {
            return null;
        }

        public Dictionary<string, string> ConstructHeaders()
        {
            var headers = new Dictionary<string, string>();

            String[] contentTypes = {
            };

            String[] accepts = {
                "application/json"
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

            if(!string.IsNullOrEmpty(Upid))
            {
                headers.Add("Upid", Upid);
            }
            if(!string.IsNullOrEmpty(Uasid))
            {
                headers.Add("Uasid", Uasid);
            }

            return headers;
        }
    }

    [Preserve]
    public class GetRoomRequest : RoomsApiBaseRequest
    {
        [Preserve]
        public string Upid { get; }
        [Preserve]
        public string Uasid { get; }
        [Preserve]
        public string RoomId { get; }
        string PathAndQueryParams;

        [Preserve]
        public GetRoomRequest(string upid = default(string), string uasid = default(string), string roomId = default(string))
        {
            Upid = upid;
            Uasid = uasid;
            RoomId = roomId;
            PathAndQueryParams = $"/{roomId}";

            List<string> queryParams = new List<string>();

            if (queryParams.Count > 0)
            {
                PathAndQueryParams = $"{PathAndQueryParams}?{string.Join("&", queryParams)}";
            }
        }

        public string ConstructUrl()
        {
            return Unity.Services.Rooms.Configuration.BasePath + PathAndQueryParams;
        }

        public byte[] ConstructBody()
        {
            return null;
        }

        public Dictionary<string, string> ConstructHeaders()
        {
            var headers = new Dictionary<string, string>();

            String[] contentTypes = {
            };

            String[] accepts = {
                "application/json"
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

            if(!string.IsNullOrEmpty(Upid))
            {
                headers.Add("Upid", Upid);
            }
            if(!string.IsNullOrEmpty(Uasid))
            {
                headers.Add("Uasid", Uasid);
            }

            return headers;
        }
    }

    [Preserve]
    public class JoinRoomRequest : RoomsApiBaseRequest
    {
        [Preserve]
        public string Upid { get; }
        [Preserve]
        public string Uasid { get; }
        [Preserve]
        public JoinRequest Body { get; }
        string PathAndQueryParams;

        [Preserve]
        public JoinRoomRequest(string upid = default(string), string uasid = default(string), JoinRequest body = default(JoinRequest))
        {
            Upid = upid;
            Uasid = uasid;
            Body = body;
            PathAndQueryParams = $"/join";

            List<string> queryParams = new List<string>();

            if (queryParams.Count > 0)
            {
                PathAndQueryParams = $"{PathAndQueryParams}?{string.Join("&", queryParams)}";
            }
        }

        public string ConstructUrl()
        {
            return Unity.Services.Rooms.Configuration.BasePath + PathAndQueryParams;
        }

        public byte[] ConstructBody()
        {
            return JsonSerialization.Serialize(Body);
        }

        public Dictionary<string, string> ConstructHeaders()
        {
            var headers = new Dictionary<string, string>();

            String[] contentTypes = {
                "application/json"
            };

            String[] accepts = {
                "application/json"
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

            if(!string.IsNullOrEmpty(Upid))
            {
                headers.Add("Upid", Upid);
            }
            if(!string.IsNullOrEmpty(Uasid))
            {
                headers.Add("Uasid", Uasid);
            }

            return headers;
        }
    }

    [Preserve]
    public class QueryRoomsRequest : RoomsApiBaseRequest
    {
        [Preserve]
        public string Upid { get; }
        [Preserve]
        public string Uasid { get; }
        [Preserve]
        public QueryRequest Body { get; }
        string PathAndQueryParams;

        [Preserve]
        public QueryRoomsRequest(string upid = default(string), string uasid = default(string), QueryRequest body = default(QueryRequest))
        {
            Upid = upid;
            Uasid = uasid;
            Body = body;
            PathAndQueryParams = $"/query";

            List<string> queryParams = new List<string>();

            if (queryParams.Count > 0)
            {
                PathAndQueryParams = $"{PathAndQueryParams}?{string.Join("&", queryParams)}";
            }
        }

        public string ConstructUrl()
        {
            return Unity.Services.Rooms.Configuration.BasePath + PathAndQueryParams;
        }

        public byte[] ConstructBody()
        {
            return JsonSerialization.Serialize(Body);
        }

        public Dictionary<string, string> ConstructHeaders()
        {
            var headers = new Dictionary<string, string>();

            String[] contentTypes = {
                "application/json"
            };

            String[] accepts = {
                "application/json"
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

            if(!string.IsNullOrEmpty(Upid))
            {
                headers.Add("Upid", Upid);
            }
            if(!string.IsNullOrEmpty(Uasid))
            {
                headers.Add("Uasid", Uasid);
            }

            return headers;
        }
    }

    [Preserve]
    public class RemovePlayerRequest : RoomsApiBaseRequest
    {
        [Preserve]
        public string Upid { get; }
        [Preserve]
        public string Uasid { get; }
        [Preserve]
        public string RoomId { get; }
        [Preserve]
        public string PlayerId { get; }
        [Preserve]
        public string Body { get; }
        string PathAndQueryParams;

        [Preserve]
        public RemovePlayerRequest(string upid = default(string), string uasid = default(string), string roomId = default(string), string playerId = default(string), string body = default(string))
        {
            Upid = upid;
            Uasid = uasid;
            RoomId = roomId;
            PlayerId = playerId;
            Body = body;
            PathAndQueryParams = $"/{roomId}/players/{playerId}";

            List<string> queryParams = new List<string>();

            if (queryParams.Count > 0)
            {
                PathAndQueryParams = $"{PathAndQueryParams}?{string.Join("&", queryParams)}";
            }
        }

        public string ConstructUrl()
        {
            return Unity.Services.Rooms.Configuration.BasePath + PathAndQueryParams;
        }

        public byte[] ConstructBody()
        {
            return JsonSerialization.Serialize(Body);
        }

        public Dictionary<string, string> ConstructHeaders()
        {
            var headers = new Dictionary<string, string>();

            String[] contentTypes = {
                "application/json"
            };

            String[] accepts = {
                "application/json"
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

            if(!string.IsNullOrEmpty(Upid))
            {
                headers.Add("Upid", Upid);
            }
            if(!string.IsNullOrEmpty(Uasid))
            {
                headers.Add("Uasid", Uasid);
            }

            return headers;
        }
    }

    [Preserve]
    public class UpdatePlayerRequest : RoomsApiBaseRequest
    {
        [Preserve]
        public string Upid { get; }
        [Preserve]
        public string Uasid { get; }
        [Preserve]
        public string RoomId { get; }
        [Preserve]
        public string PlayerId { get; }
        [Preserve]
        public PlayerUpdateRequest Body { get; }
        string PathAndQueryParams;

        [Preserve]
        public UpdatePlayerRequest(string upid = default(string), string uasid = default(string), string roomId = default(string), string playerId = default(string), PlayerUpdateRequest body = default(PlayerUpdateRequest))
        {
            Upid = upid;
            Uasid = uasid;
            RoomId = roomId;
            PlayerId = playerId;
            Body = body;
            PathAndQueryParams = $"/{roomId}/players/{playerId}";

            List<string> queryParams = new List<string>();

            if (queryParams.Count > 0)
            {
                PathAndQueryParams = $"{PathAndQueryParams}?{string.Join("&", queryParams)}";
            }
        }

        public string ConstructUrl()
        {
            return Unity.Services.Rooms.Configuration.BasePath + PathAndQueryParams;
        }

        public byte[] ConstructBody()
        {
            return JsonSerialization.Serialize(Body);
        }

        public Dictionary<string, string> ConstructHeaders()
        {
            var headers = new Dictionary<string, string>();

            String[] contentTypes = {
                "application/json"
            };

            String[] accepts = {
                "application/json"
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

            if(!string.IsNullOrEmpty(Upid))
            {
                headers.Add("Upid", Upid);
            }
            if(!string.IsNullOrEmpty(Uasid))
            {
                headers.Add("Uasid", Uasid);
            }

            return headers;
        }
    }

    [Preserve]
    public class UpdateRoomRequest : RoomsApiBaseRequest
    {
        [Preserve]
        public string Upid { get; }
        [Preserve]
        public string Uasid { get; }
        [Preserve]
        public string RoomId { get; }
        [Preserve]
        public UpdateRequest Body { get; }
        string PathAndQueryParams;

        [Preserve]
        public UpdateRoomRequest(string upid = default(string), string uasid = default(string), string roomId = default(string), UpdateRequest body = default(UpdateRequest))
        {
            Upid = upid;
            Uasid = uasid;
            RoomId = roomId;
            Body = body;
            PathAndQueryParams = $"/{roomId}";

            List<string> queryParams = new List<string>();

            if (queryParams.Count > 0)
            {
                PathAndQueryParams = $"{PathAndQueryParams}?{string.Join("&", queryParams)}";
            }
        }

        public string ConstructUrl()
        {
            return Unity.Services.Rooms.Configuration.BasePath + PathAndQueryParams;
        }

        public byte[] ConstructBody()
        {
            return JsonSerialization.Serialize(Body);
        }

        public Dictionary<string, string> ConstructHeaders()
        {
            var headers = new Dictionary<string, string>();

            String[] contentTypes = {
                "application/json"
            };

            String[] accepts = {
                "application/json"
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

            if(!string.IsNullOrEmpty(Upid))
            {
                headers.Add("Upid", Upid);
            }
            if(!string.IsNullOrEmpty(Uasid))
            {
                headers.Add("Uasid", Uasid);
            }

            return headers;
        }
    }
}