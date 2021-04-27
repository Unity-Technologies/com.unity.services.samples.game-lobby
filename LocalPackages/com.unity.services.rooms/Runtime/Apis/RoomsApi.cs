using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Unity.Services.Rooms.Models;
using Unity.Services.Rooms.Http;
using TaskScheduler = Unity.Services.Rooms.Scheduler.TaskScheduler;
using Unity.Services.Rooms.Rooms;

namespace Unity.Services.Rooms.Apis.Rooms
{
    public interface IRoomsApiClient
    {
            Task<Response<Room>> CreateRoomAsync(CreateRoomRequest request);
            Task<Response> DeleteRoomAsync(DeleteRoomRequest request);
            Task<Response<Room>> GetRoomAsync(GetRoomRequest request);
            Task<Response<Room>> JoinRoomAsync(JoinRoomRequest request);
            Task<Response<QueryResponse>> QueryRoomsAsync(QueryRoomsRequest request);
            Task<Response> RemovePlayerAsync(RemovePlayerRequest request);
            Task<Response<Room>> UpdatePlayerAsync(UpdatePlayerRequest request);
            Task<Response<Room>> UpdateRoomAsync(UpdateRoomRequest request);
    }

    internal class RoomsApiClient : BaseApiClient, IRoomsApiClient
    {
        internal RoomsApiClient(IHttpClient httpClient, TaskScheduler taskScheduler) : base(httpClient, taskScheduler)
        {
        }

        public async Task<Response<Room>> CreateRoomAsync(CreateRoomRequest request)
        {
            var response = await HttpClient.MakeRequestAsync("POST", request.ConstructUrl(), request.ConstructBody(), request.ConstructHeaders());
            return new Response<Room>(response, ResponseHandler.HandleAsyncResponse<Room>(response));
        }
        public async Task<Response> DeleteRoomAsync(DeleteRoomRequest request)
        {
            var response = await HttpClient.MakeRequestAsync("DELETE", request.ConstructUrl(), request.ConstructBody(), request.ConstructHeaders());
            return new Response(response);
        }
        public async Task<Response<Room>> GetRoomAsync(GetRoomRequest request)
        {
            var response = await HttpClient.MakeRequestAsync("GET", request.ConstructUrl(), request.ConstructBody(), request.ConstructHeaders());
            return new Response<Room>(response, ResponseHandler.HandleAsyncResponse<Room>(response));
        }
        public async Task<Response<Room>> JoinRoomAsync(JoinRoomRequest request)
        {
            var response = await HttpClient.MakeRequestAsync("POST", request.ConstructUrl(), request.ConstructBody(), request.ConstructHeaders());
            return new Response<Room>(response, ResponseHandler.HandleAsyncResponse<Room>(response));
        }
        public async Task<Response<QueryResponse>> QueryRoomsAsync(QueryRoomsRequest request)
        {
            var response = await HttpClient.MakeRequestAsync("POST", request.ConstructUrl(), request.ConstructBody(), request.ConstructHeaders());
            return new Response<QueryResponse>(response, ResponseHandler.HandleAsyncResponse<QueryResponse>(response));
        }
        public async Task<Response> RemovePlayerAsync(RemovePlayerRequest request)
        {
            var response = await HttpClient.MakeRequestAsync("DELETE", request.ConstructUrl(), request.ConstructBody(), request.ConstructHeaders());
            return new Response(response);
        }
        public async Task<Response<Room>> UpdatePlayerAsync(UpdatePlayerRequest request)
        {
            var response = await HttpClient.MakeRequestAsync("POST", request.ConstructUrl(), request.ConstructBody(), request.ConstructHeaders());
            return new Response<Room>(response, ResponseHandler.HandleAsyncResponse<Room>(response));
        }
        public async Task<Response<Room>> UpdateRoomAsync(UpdateRoomRequest request)
        {
            var response = await HttpClient.MakeRequestAsync("POST", request.ConstructUrl(), request.ConstructBody(), request.ConstructHeaders());
            return new Response<Room>(response, ResponseHandler.HandleAsyncResponse<Room>(response));
        }
    }
}
