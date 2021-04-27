# Documentation for Unity Rooms
    <a name="documentation-for-api-endpoints"></a>
    ## Documentation for API Endpoints
    All URIs are relative to *https://rooms.cloud.unity3d.com/api/v1/rooms*
    Class | Method | HTTP request | Description
    ------------ | ------------- | ------------- | -------------
    *RoomsApi* | [**CreateRoom**](Apis/RoomsApi.md#createroom) | **POST** /create | Creates a new Room.
    *RoomsApi* | [**DeleteRoom**](Apis/RoomsApi.md#deleteroom) | **DELETE** /{roomId} | Remove a room.
    *RoomsApi* | [**GetRoom**](Apis/RoomsApi.md#getroom) | **GET** /{roomId} | Get the details for a room
    *RoomsApi* | [**JoinRoom**](Apis/RoomsApi.md#joinroom) | **POST** /join | Join a room.
    *RoomsApi* | [**QueryRooms**](Apis/RoomsApi.md#queryrooms) | **POST** /query | Query available rooms.
    *RoomsApi* | [**RemovePlayer**](Apis/RoomsApi.md#removeplayer) | **DELETE** /{roomId}/players/{playerId} | Remove a player
    *RoomsApi* | [**UpdatePlayer**](Apis/RoomsApi.md#updateplayer) | **POST** /{roomId}/players/{playerId} | Update player data.
    *RoomsApi* | [**UpdateRoom**](Apis/RoomsApi.md#updateroom) | **POST** /{roomId} | Update the data for a room.
    
    <a name="documentation-for-models"></a>
    ## Documentation for Models
         - [Models.CreateRequest](Models/CreateRequest.md)
         - [Models.DataObject](Models/DataObject.md)
         - [Models.Detail](Models/Detail.md)
         - [Models.ErrorStatus](Models/ErrorStatus.md)
         - [Models.JoinRequest](Models/JoinRequest.md)
         - [Models.Player](Models/Player.md)
         - [Models.PlayerUpdateRequest](Models/PlayerUpdateRequest.md)
         - [Models.QueryFilter](Models/QueryFilter.md)
         - [Models.QueryRequest](Models/QueryRequest.md)
         - [Models.QueryResponse](Models/QueryResponse.md)
         - [Models.Room](Models/Room.md)
         - [Models.UpdateRequest](Models/UpdateRequest.md)
        
<a name="documentation-for-authorization"></a>
## Documentation for Authorization
    All endpoints do not require authorization.
