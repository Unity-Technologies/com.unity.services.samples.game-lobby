using System.Threading.Tasks;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies.Http;
using TaskScheduler = Unity.Services.Lobbies.Scheduler.TaskScheduler;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Lobby;

namespace Unity.Services.Lobbies.Apis.Lobby
{
    public interface ILobbyApiClient
    {
            /// <summary>
            /// Async Operation.
            /// Create a lobby
            /// </summary>
            /// <param name="request">Request object for CreateLobby</param>
            /// <param name="operationConfiguration">Configuration for CreateLobby</param>
            /// <returns>Task for a Response object containing status code, headers, and Lobby object</returns>
            /// <exception cref="Unity.Services.Lobbies.Http.HttpException">An exception containing the HttpClientResponse with headers, response code, and string of error.</exception>
            Task<Response<Models.Lobby>> CreateLobbyAsync(CreateLobbyRequest request, Configuration operationConfiguration = null);

            /// <summary>
            /// Async Operation.
            /// Delete a lobby
            /// </summary>
            /// <param name="request">Request object for DeleteLobby</param>
            /// <param name="operationConfiguration">Configuration for DeleteLobby</param>
            /// <returns>Task for a Response object containing status code, headers</returns>
            /// <exception cref="Unity.Services.Lobbies.Http.HttpException">An exception containing the HttpClientResponse with headers, response code, and string of error.</exception>
            Task<Response> DeleteLobbyAsync(DeleteLobbyRequest request, Configuration operationConfiguration = null);

            /// <summary>
            /// Async Operation.
            /// Get lobby details
            /// </summary>
            /// <param name="request">Request object for GetLobby</param>
            /// <param name="operationConfiguration">Configuration for GetLobby</param>
            /// <returns>Task for a Response object containing status code, headers, and Lobby object</returns>
            /// <exception cref="Unity.Services.Lobbies.Http.HttpException">An exception containing the HttpClientResponse with headers, response code, and string of error.</exception>
            Task<Response<Models.Lobby>> GetLobbyAsync(GetLobbyRequest request, Configuration operationConfiguration = null);

            /// <summary>
            /// Async Operation.
            /// Heartbeat a lobby
            /// </summary>
            /// <param name="request">Request object for Heartbeat</param>
            /// <param name="operationConfiguration">Configuration for Heartbeat</param>
            /// <returns>Task for a Response object containing status code, headers</returns>
            /// <exception cref="Unity.Services.Lobbies.Http.HttpException">An exception containing the HttpClientResponse with headers, response code, and string of error.</exception>
            Task<Response> HeartbeatAsync(HeartbeatRequest request, Configuration operationConfiguration = null);

            /// <summary>
            /// Async Operation.
            /// Join a lobby with lobby code
            /// </summary>
            /// <param name="request">Request object for JoinLobbyByCode</param>
            /// <param name="operationConfiguration">Configuration for JoinLobbyByCode</param>
            /// <returns>Task for a Response object containing status code, headers, and Lobby object</returns>
            /// <exception cref="Unity.Services.Lobbies.Http.HttpException">An exception containing the HttpClientResponse with headers, response code, and string of error.</exception>
            Task<Response<Models.Lobby>> JoinLobbyByCodeAsync(JoinLobbyByCodeRequest request, Configuration operationConfiguration = null);

            /// <summary>
            /// Async Operation.
            /// Join a lobby with lobby ID
            /// </summary>
            /// <param name="request">Request object for JoinLobbyById</param>
            /// <param name="operationConfiguration">Configuration for JoinLobbyById</param>
            /// <returns>Task for a Response object containing status code, headers, and Lobby object</returns>
            /// <exception cref="Unity.Services.Lobbies.Http.HttpException">An exception containing the HttpClientResponse with headers, response code, and string of error.</exception>
            Task<Response<Models.Lobby>> JoinLobbyByIdAsync(JoinLobbyByIdRequest request, Configuration operationConfiguration = null);

            /// <summary>
            /// Async Operation.
            /// Query public lobbies
            /// </summary>
            /// <param name="request">Request object for QueryLobbies</param>
            /// <param name="operationConfiguration">Configuration for QueryLobbies</param>
            /// <returns>Task for a Response object containing status code, headers, and QueryResponse object</returns>
            /// <exception cref="Unity.Services.Lobbies.Http.HttpException">An exception containing the HttpClientResponse with headers, response code, and string of error.</exception>
            Task<Response<QueryResponse>> QueryLobbiesAsync(QueryLobbiesRequest request, Configuration operationConfiguration = null);

            /// <summary>
            /// Async Operation.
            /// Query available lobbies and join a random one
            /// </summary>
            /// <param name="request">Request object for QuickJoinLobby</param>
            /// <param name="operationConfiguration">Configuration for QuickJoinLobby</param>
            /// <returns>Task for a Response object containing status code, headers, and Lobby object</returns>
            /// <exception cref="Unity.Services.Lobbies.Http.HttpException">An exception containing the HttpClientResponse with headers, response code, and string of error.</exception>
            Task<Response<Models.Lobby>> QuickJoinLobbyAsync(QuickJoinLobbyRequest request, Configuration operationConfiguration = null);

            /// <summary>
            /// Async Operation.
            /// Remove a player
            /// </summary>
            /// <param name="request">Request object for RemovePlayer</param>
            /// <param name="operationConfiguration">Configuration for RemovePlayer</param>
            /// <returns>Task for a Response object containing status code, headers</returns>
            /// <exception cref="Unity.Services.Lobbies.Http.HttpException">An exception containing the HttpClientResponse with headers, response code, and string of error.</exception>
            Task<Response> RemovePlayerAsync(RemovePlayerRequest request, Configuration operationConfiguration = null);

            /// <summary>
            /// Async Operation.
            /// Update lobby data
            /// </summary>
            /// <param name="request">Request object for UpdateLobby</param>
            /// <param name="operationConfiguration">Configuration for UpdateLobby</param>
            /// <returns>Task for a Response object containing status code, headers, and Lobby object</returns>
            /// <exception cref="Unity.Services.Lobbies.Http.HttpException">An exception containing the HttpClientResponse with headers, response code, and string of error.</exception>
            Task<Response<Models.Lobby>> UpdateLobbyAsync(UpdateLobbyRequest request, Configuration operationConfiguration = null);

            /// <summary>
            /// Async Operation.
            /// Update player data
            /// </summary>
            /// <param name="request">Request object for UpdatePlayer</param>
            /// <param name="operationConfiguration">Configuration for UpdatePlayer</param>
            /// <returns>Task for a Response object containing status code, headers, and Lobby object</returns>
            /// <exception cref="Unity.Services.Lobbies.Http.HttpException">An exception containing the HttpClientResponse with headers, response code, and string of error.</exception>
            Task<Response<Models.Lobby>> UpdatePlayerAsync(UpdatePlayerRequest request, Configuration operationConfiguration = null);

    }

    ///<inheritdoc cref="ILobbyApiClient"/>
    public class LobbyApiClient : BaseApiClient, ILobbyApiClient
    {
        private IAccessToken _accessToken;
        private const int _baseTimeout = 10;
        private Configuration _configuration;
        public Configuration Configuration
        {
            get {
                // We return a merge between the current configuration and the
                // global configuration to ensure we have the correct
                // combination of headers and a base path (if it is set).
                return Configuration.MergeConfigurations(_configuration, LobbyService.Configuration);
            }
        }

        public LobbyApiClient(IHttpClient httpClient,
            IAccessToken accessToken,
            Configuration configuration = null) : base(httpClient)
        {
            // We don't need to worry about the configuration being null at
            // this stage, we will check this in the accessor.
            _configuration = configuration;

            _accessToken = accessToken;
        }

        public async Task<Response<Models.Lobby>> CreateLobbyAsync(CreateLobbyRequest request,
            Configuration operationConfiguration = null)
        {
            var statusCodeToTypeMap = new Dictionary<string, System.Type>() { { "201", typeof(Models.Lobby) },{ "400", typeof(ErrorStatus) },{ "403", typeof(ErrorStatus) } };

            // Merge the operation/request level configuration with the client level configuration.
            var finalConfiguration = Configuration.MergeConfigurations(operationConfiguration, Configuration);

            var response = await HttpClient.MakeRequestAsync("POST",
                request.ConstructUrl(finalConfiguration.BasePath),
                request.ConstructBody(),
                request.ConstructHeaders(_accessToken, finalConfiguration),
                finalConfiguration.RequestTimeout ?? _baseTimeout);

            var handledResponse = ResponseHandler.HandleAsyncResponse<Models.Lobby>(response, statusCodeToTypeMap);
            return new Response<Models.Lobby>(response, handledResponse);
        }

        public async Task<Response> DeleteLobbyAsync(DeleteLobbyRequest request,
            Configuration operationConfiguration = null)
        {
            var statusCodeToTypeMap = new Dictionary<string, System.Type>() { { "204", null },{ "400", typeof(ErrorStatus) },{ "403", typeof(ErrorStatus) },{ "404", typeof(ErrorStatus) } };

            // Merge the operation/request level configuration with the client level configuration.
            var finalConfiguration = Configuration.MergeConfigurations(operationConfiguration, Configuration);

            var response = await HttpClient.MakeRequestAsync("DELETE",
                request.ConstructUrl(finalConfiguration.BasePath),
                request.ConstructBody(),
                request.ConstructHeaders(_accessToken, finalConfiguration),
                finalConfiguration.RequestTimeout ?? _baseTimeout);

            ResponseHandler.HandleAsyncResponse(response, statusCodeToTypeMap);
            return new Response(response);
        }

        public async Task<Response<Models.Lobby>> GetLobbyAsync(GetLobbyRequest request,
            Configuration operationConfiguration = null)
        {
            var statusCodeToTypeMap = new Dictionary<string, System.Type>() { { "200", typeof(Models.Lobby) },{ "400", typeof(ErrorStatus) },{ "403", typeof(ErrorStatus) },{ "404", typeof(ErrorStatus) } };

            // Merge the operation/request level configuration with the client level configuration.
            var finalConfiguration = Configuration.MergeConfigurations(operationConfiguration, Configuration);

            var response = await HttpClient.MakeRequestAsync("GET",
                request.ConstructUrl(finalConfiguration.BasePath),
                request.ConstructBody(),
                request.ConstructHeaders(_accessToken, finalConfiguration),
                finalConfiguration.RequestTimeout ?? _baseTimeout);

            var handledResponse = ResponseHandler.HandleAsyncResponse<Models.Lobby>(response, statusCodeToTypeMap);
            return new Response<Models.Lobby>(response, handledResponse);
        }

        public async Task<Response> HeartbeatAsync(HeartbeatRequest request,
            Configuration operationConfiguration = null)
        {
            var statusCodeToTypeMap = new Dictionary<string, System.Type>() { { "204", null },{ "400", typeof(ErrorStatus) },{ "403", typeof(ErrorStatus) },{ "404", typeof(ErrorStatus) } };

            // Merge the operation/request level configuration with the client level configuration.
            var finalConfiguration = Configuration.MergeConfigurations(operationConfiguration, Configuration);

            var response = await HttpClient.MakeRequestAsync("POST",
                request.ConstructUrl(finalConfiguration.BasePath),
                request.ConstructBody(),
                request.ConstructHeaders(_accessToken, finalConfiguration),
                finalConfiguration.RequestTimeout ?? _baseTimeout);

            ResponseHandler.HandleAsyncResponse(response, statusCodeToTypeMap);
            return new Response(response);
        }

        public async Task<Response<Models.Lobby>> JoinLobbyByCodeAsync(JoinLobbyByCodeRequest request,
            Configuration operationConfiguration = null)
        {
            var statusCodeToTypeMap = new Dictionary<string, System.Type>() { { "200", typeof(Models.Lobby) },{ "400", typeof(ErrorStatus) },{ "403", typeof(ErrorStatus) },{ "409", typeof(ErrorStatus) } };

            // Merge the operation/request level configuration with the client level configuration.
            var finalConfiguration = Configuration.MergeConfigurations(operationConfiguration, Configuration);

            var response = await HttpClient.MakeRequestAsync("POST",
                request.ConstructUrl(finalConfiguration.BasePath),
                request.ConstructBody(),
                request.ConstructHeaders(_accessToken, finalConfiguration),
                finalConfiguration.RequestTimeout ?? _baseTimeout);

            var handledResponse = ResponseHandler.HandleAsyncResponse<Models.Lobby>(response, statusCodeToTypeMap);
            return new Response<Models.Lobby>(response, handledResponse);
        }

        public async Task<Response<Models.Lobby>> JoinLobbyByIdAsync(JoinLobbyByIdRequest request,
            Configuration operationConfiguration = null)
        {
            var statusCodeToTypeMap = new Dictionary<string, System.Type>() { { "200", typeof(Models.Lobby) },{ "400", typeof(ErrorStatus) },{ "403", typeof(ErrorStatus) },{ "404", typeof(ErrorStatus) },{ "409", typeof(ErrorStatus) } };

            // Merge the operation/request level configuration with the client level configuration.
            var finalConfiguration = Configuration.MergeConfigurations(operationConfiguration, Configuration);

            var response = await HttpClient.MakeRequestAsync("POST",
                request.ConstructUrl(finalConfiguration.BasePath),
                request.ConstructBody(),
                request.ConstructHeaders(_accessToken, finalConfiguration),
                finalConfiguration.RequestTimeout ?? _baseTimeout);

            var handledResponse = ResponseHandler.HandleAsyncResponse<Models.Lobby>(response, statusCodeToTypeMap);
            return new Response<Models.Lobby>(response, handledResponse);
        }

        public async Task<Response<QueryResponse>> QueryLobbiesAsync(QueryLobbiesRequest request,
            Configuration operationConfiguration = null)
        {
            var statusCodeToTypeMap = new Dictionary<string, System.Type>() { { "200", typeof(QueryResponse) },{ "400", typeof(ErrorStatus) },{ "403", typeof(ErrorStatus) } };

            // Merge the operation/request level configuration with the client level configuration.
            var finalConfiguration = Configuration.MergeConfigurations(operationConfiguration, Configuration);

            var response = await HttpClient.MakeRequestAsync("POST",
                request.ConstructUrl(finalConfiguration.BasePath),
                request.ConstructBody(),
                request.ConstructHeaders(_accessToken, finalConfiguration),
                finalConfiguration.RequestTimeout ?? _baseTimeout);

            var handledResponse = ResponseHandler.HandleAsyncResponse<QueryResponse>(response, statusCodeToTypeMap);
            return new Response<QueryResponse>(response, handledResponse);
        }

        public async Task<Response<Models.Lobby>> QuickJoinLobbyAsync(QuickJoinLobbyRequest request,
            Configuration operationConfiguration = null)
        {
            var statusCodeToTypeMap = new Dictionary<string, System.Type>() { { "200", typeof(Models.Lobby) },{ "400", typeof(ErrorStatus) },{ "403", typeof(ErrorStatus) },{ "404", typeof(ErrorStatus) },{ "409", typeof(ErrorStatus) } };

            // Merge the operation/request level configuration with the client level configuration.
            var finalConfiguration = Configuration.MergeConfigurations(operationConfiguration, Configuration);

            var response = await HttpClient.MakeRequestAsync("POST",
                request.ConstructUrl(finalConfiguration.BasePath),
                request.ConstructBody(),
                request.ConstructHeaders(_accessToken, finalConfiguration),
                finalConfiguration.RequestTimeout ?? _baseTimeout);

            var handledResponse = ResponseHandler.HandleAsyncResponse<Models.Lobby>(response, statusCodeToTypeMap);
            return new Response<Models.Lobby>(response, handledResponse);
        }

        public async Task<Response> RemovePlayerAsync(RemovePlayerRequest request,
            Configuration operationConfiguration = null)
        {
            var statusCodeToTypeMap = new Dictionary<string, System.Type>() { { "204", null },{ "400", typeof(ErrorStatus) },{ "403", typeof(ErrorStatus) },{ "404", typeof(ErrorStatus) } };

            // Merge the operation/request level configuration with the client level configuration.
            var finalConfiguration = Configuration.MergeConfigurations(operationConfiguration, Configuration);

            var response = await HttpClient.MakeRequestAsync("DELETE",
                request.ConstructUrl(finalConfiguration.BasePath),
                request.ConstructBody(),
                request.ConstructHeaders(_accessToken, finalConfiguration),
                finalConfiguration.RequestTimeout ?? _baseTimeout);

            ResponseHandler.HandleAsyncResponse(response, statusCodeToTypeMap);
            return new Response(response);
        }

        public async Task<Response<Models.Lobby>> UpdateLobbyAsync(UpdateLobbyRequest request,
            Configuration operationConfiguration = null)
        {
            var statusCodeToTypeMap = new Dictionary<string, System.Type>() { { "200", typeof(Models.Lobby) },{ "400", typeof(ErrorStatus) },{ "403", typeof(ErrorStatus) },{ "404", typeof(ErrorStatus) } };

            // Merge the operation/request level configuration with the client level configuration.
            var finalConfiguration = Configuration.MergeConfigurations(operationConfiguration, Configuration);

            var response = await HttpClient.MakeRequestAsync("POST",
                request.ConstructUrl(finalConfiguration.BasePath),
                request.ConstructBody(),
                request.ConstructHeaders(_accessToken, finalConfiguration),
                finalConfiguration.RequestTimeout ?? _baseTimeout);

            var handledResponse = ResponseHandler.HandleAsyncResponse<Models.Lobby>(response, statusCodeToTypeMap);
            return new Response<Models.Lobby>(response, handledResponse);
        }

        public async Task<Response<Models.Lobby>> UpdatePlayerAsync(UpdatePlayerRequest request,
            Configuration operationConfiguration = null)
        {
            var statusCodeToTypeMap = new Dictionary<string, System.Type>() { { "200", typeof(Models.Lobby) },{ "400", typeof(ErrorStatus) },{ "403", typeof(ErrorStatus) },{ "404", typeof(ErrorStatus) } };

            // Merge the operation/request level configuration with the client level configuration.
            var finalConfiguration = Configuration.MergeConfigurations(operationConfiguration, Configuration);

            var response = await HttpClient.MakeRequestAsync("POST",
                request.ConstructUrl(finalConfiguration.BasePath),
                request.ConstructBody(),
                request.ConstructHeaders(_accessToken, finalConfiguration),
                finalConfiguration.RequestTimeout ?? _baseTimeout);

            var handledResponse = ResponseHandler.HandleAsyncResponse<Models.Lobby>(response, statusCodeToTypeMap);
            return new Response<Models.Lobby>(response, handledResponse);
        }

    }
}
