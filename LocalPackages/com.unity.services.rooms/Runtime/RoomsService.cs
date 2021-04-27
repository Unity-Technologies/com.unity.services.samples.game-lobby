using Unity.Services.Rooms.Apis.Rooms;


namespace Unity.Services.Rooms
{
    public static class RoomsService
    {
        public static IRoomsApiClient RoomsApiClient { get; internal set; }
        
    }
}