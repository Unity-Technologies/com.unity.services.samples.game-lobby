using System.Threading.Tasks;
using System.Collections.Generic;
using Unity.Services.Relay.Models;
using Unity.Services.Relay.Http;
using TaskScheduler = Unity.Services.Relay.Scheduler.TaskScheduler;
using Unity.Services.Authentication;
using Unity.Services.Relay.Allocations;

namespace Unity.Services.Relay.Apis.Allocations
{
    public interface IAllocationsApiClient
    {
            /// <summary>
            /// Async Operation.
            /// Create Allocation
            /// </summary>
            /// <param name="request">Request object for CreateAllocation</param>
            /// <param name="operationConfiguration">Configuration for CreateAllocation</param>
            /// <returns>Task for a Response object containing status code, headers, and AllocateResponseBody object</returns>
            /// <exception cref="Unity.Services.Relay.Http.HttpException">An exception containing the HttpClientResponse with headers, response code, and string of error.</exception>
            Task<Response<AllocateResponseBody>> CreateAllocationAsync(CreateAllocationRequest request, Configuration operationConfiguration = null);

            /// <summary>
            /// Async Operation.
            /// Create Join Code
            /// </summary>
            /// <param name="request">Request object for CreateJoincode</param>
            /// <param name="operationConfiguration">Configuration for CreateJoincode</param>
            /// <returns>Task for a Response object containing status code, headers, and JoinCodeResponseBody object</returns>
            /// <exception cref="Unity.Services.Relay.Http.HttpException">An exception containing the HttpClientResponse with headers, response code, and string of error.</exception>
            Task<Response<JoinCodeResponseBody>> CreateJoincodeAsync(CreateJoincodeRequest request, Configuration operationConfiguration = null);

            /// <summary>
            /// Async Operation.
            /// Join Relay
            /// </summary>
            /// <param name="request">Request object for JoinRelay</param>
            /// <param name="operationConfiguration">Configuration for JoinRelay</param>
            /// <returns>Task for a Response object containing status code, headers, and JoinResponseBody object</returns>
            /// <exception cref="Unity.Services.Relay.Http.HttpException">An exception containing the HttpClientResponse with headers, response code, and string of error.</exception>
            Task<Response<JoinResponseBody>> JoinRelayAsync(JoinRelayRequest request, Configuration operationConfiguration = null);

            /// <summary>
            /// Async Operation.
            /// List relay regions
            /// </summary>
            /// <param name="request">Request object for ListRegions</param>
            /// <param name="operationConfiguration">Configuration for ListRegions</param>
            /// <returns>Task for a Response object containing status code, headers, and RegionsResponseBody object</returns>
            /// <exception cref="Unity.Services.Relay.Http.HttpException">An exception containing the HttpClientResponse with headers, response code, and string of error.</exception>
            Task<Response<RegionsResponseBody>> ListRegionsAsync(ListRegionsRequest request, Configuration operationConfiguration = null);

    }

    ///<inheritdoc cref="IAllocationsApiClient"/>
    public class AllocationsApiClient : BaseApiClient, IAllocationsApiClient
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
                return Configuration.MergeConfigurations(_configuration, RelayService.Configuration);
            }
        }

        public AllocationsApiClient(IHttpClient httpClient,
            IAccessToken accessToken,
            Configuration configuration = null) : base(httpClient)
        {
            // We don't need to worry about the configuration being null at
            // this stage, we will check this in the accessor.
            _configuration = configuration;

            _accessToken = accessToken;
        }

        public async Task<Response<AllocateResponseBody>> CreateAllocationAsync(CreateAllocationRequest request,
            Configuration operationConfiguration = null)
        {
            var statusCodeToTypeMap = new Dictionary<string, System.Type>() { { "201", typeof(AllocateResponseBody) },{ "400", typeof(ErrorResponseBody) },{ "401", typeof(ErrorResponseBody) },{ "403", typeof(ErrorResponseBody) },{ "500", typeof(ErrorResponseBody) } };

            // Merge the operation/request level configuration with the client level configuration.
            var finalConfiguration = Configuration.MergeConfigurations(operationConfiguration, Configuration);

            var response = await HttpClient.MakeRequestAsync("POST",
                request.ConstructUrl(finalConfiguration.BasePath),
                request.ConstructBody(),
                request.ConstructHeaders(_accessToken, finalConfiguration),
                finalConfiguration.RequestTimeout ?? _baseTimeout);

            var handledResponse = ResponseHandler.HandleAsyncResponse<AllocateResponseBody>(response, statusCodeToTypeMap);
            return new Response<AllocateResponseBody>(response, handledResponse);
        }

        public async Task<Response<JoinCodeResponseBody>> CreateJoincodeAsync(CreateJoincodeRequest request,
            Configuration operationConfiguration = null)
        {
            var statusCodeToTypeMap = new Dictionary<string, System.Type>() { { "200", typeof(JoinCodeResponseBody) },{ "201", typeof(JoinCodeResponseBody) },{ "400", typeof(ErrorResponseBody) },{ "401", typeof(ErrorResponseBody) },{ "403", typeof(ErrorResponseBody) },{ "500", typeof(ErrorResponseBody) } };

            // Merge the operation/request level configuration with the client level configuration.
            var finalConfiguration = Configuration.MergeConfigurations(operationConfiguration, Configuration);

            var response = await HttpClient.MakeRequestAsync("POST",
                request.ConstructUrl(finalConfiguration.BasePath),
                request.ConstructBody(),
                request.ConstructHeaders(_accessToken, finalConfiguration),
                finalConfiguration.RequestTimeout ?? _baseTimeout);

            var handledResponse = ResponseHandler.HandleAsyncResponse<JoinCodeResponseBody>(response, statusCodeToTypeMap);
            return new Response<JoinCodeResponseBody>(response, handledResponse);
        }

        public async Task<Response<JoinResponseBody>> JoinRelayAsync(JoinRelayRequest request,
            Configuration operationConfiguration = null)
        {
            var statusCodeToTypeMap = new Dictionary<string, System.Type>() { { "200", typeof(JoinResponseBody) },{ "400", typeof(ErrorResponseBody) },{ "401", typeof(ErrorResponseBody) },{ "403", typeof(ErrorResponseBody) },{ "404", typeof(ErrorResponseBody) },{ "500", typeof(ErrorResponseBody) } };

            // Merge the operation/request level configuration with the client level configuration.
            var finalConfiguration = Configuration.MergeConfigurations(operationConfiguration, Configuration);

            var response = await HttpClient.MakeRequestAsync("POST",
                request.ConstructUrl(finalConfiguration.BasePath),
                request.ConstructBody(),
                request.ConstructHeaders(_accessToken, finalConfiguration),
                finalConfiguration.RequestTimeout ?? _baseTimeout);

            var handledResponse = ResponseHandler.HandleAsyncResponse<JoinResponseBody>(response, statusCodeToTypeMap);
            return new Response<JoinResponseBody>(response, handledResponse);
        }

        public async Task<Response<RegionsResponseBody>> ListRegionsAsync(ListRegionsRequest request,
            Configuration operationConfiguration = null)
        {
            var statusCodeToTypeMap = new Dictionary<string, System.Type>() { { "200", typeof(RegionsResponseBody) },{ "400", typeof(ErrorResponseBody) },{ "401", typeof(ErrorResponseBody) },{ "403", typeof(ErrorResponseBody) },{ "500", typeof(ErrorResponseBody) } };

            // Merge the operation/request level configuration with the client level configuration.
            var finalConfiguration = Configuration.MergeConfigurations(operationConfiguration, Configuration);

            var response = await HttpClient.MakeRequestAsync("GET",
                request.ConstructUrl(finalConfiguration.BasePath),
                request.ConstructBody(),
                request.ConstructHeaders(_accessToken, finalConfiguration),
                finalConfiguration.RequestTimeout ?? _baseTimeout);

            var handledResponse = ResponseHandler.HandleAsyncResponse<RegionsResponseBody>(response, statusCodeToTypeMap);
            return new Response<RegionsResponseBody>(response, handledResponse);
        }

    }
}
