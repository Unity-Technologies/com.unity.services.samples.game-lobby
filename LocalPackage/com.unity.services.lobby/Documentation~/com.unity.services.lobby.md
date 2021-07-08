# Documentation for Unity Lobby
    <a name="documentation-for-api-endpoints"></a>
    ## Documentation for API Endpoints
    All URIs are relative to *https://lobby.cloud.unity3d.com/v1*
    Class | Method | HTTP request | Description
    ------------ | ------------- | ------------- | -------------
    *LobbyApi* | [**CreateLobby**](Apis/LobbyApi.md#createlobby) | **POST** /create | Create a lobby
    *LobbyApi* | [**DeleteLobby**](Apis/LobbyApi.md#deletelobby) | **DELETE** /{lobbyId} | Delete a lobby
    *LobbyApi* | [**GetLobby**](Apis/LobbyApi.md#getlobby) | **GET** /{lobbyId} | Get lobby details
    *LobbyApi* | [**JoinLobbyByCode**](Apis/LobbyApi.md#joinlobbybycode) | **POST** /joinbycode | Join a lobby with lobby code
    *LobbyApi* | [**JoinLobbyById**](Apis/LobbyApi.md#joinlobbybyid) | **POST** /{lobbyId}/join | Join a lobby with lobby ID
    *LobbyApi* | [**QueryLobbies**](Apis/LobbyApi.md#querylobbies) | **POST** /query | Query public lobbies
    *LobbyApi* | [**QuickJoinLobby**](Apis/LobbyApi.md#quickjoinlobby) | **POST** /quickjoin | Query available lobbies and join a random one
    *LobbyApi* | [**RemovePlayer**](Apis/LobbyApi.md#removeplayer) | **DELETE** /{lobbyId}/players/{playerId} | Remove a player
    *LobbyApi* | [**UpdateLobby**](Apis/LobbyApi.md#updatelobby) | **POST** /{lobbyId} | Update lobby data
    *LobbyApi* | [**UpdatePlayer**](Apis/LobbyApi.md#updateplayer) | **POST** /{lobbyId}/players/{playerId} | Update player data
    
    <a name="documentation-for-models"></a>
    ## Documentation for Models
         - [Models.CreateRequest](Models/CreateRequest.md)
         - [Models.DataObject](Models/DataObject.md)
         - [Models.Detail](Models/Detail.md)
         - [Models.ErrorStatus](Models/ErrorStatus.md)
         - [Models.JoinByCodeRequest](Models/JoinByCodeRequest.md)
         - [Models.Lobby](Models/Lobby.md)
         - [Models.Player](Models/Player.md)
         - [Models.PlayerDataObject](Models/PlayerDataObject.md)
         - [Models.PlayerUpdateRequest](Models/PlayerUpdateRequest.md)
         - [Models.QueryFilter](Models/QueryFilter.md)
         - [Models.QueryOrder](Models/QueryOrder.md)
         - [Models.QueryRequest](Models/QueryRequest.md)
         - [Models.QueryResponse](Models/QueryResponse.md)
         - [Models.QuickJoinRequest](Models/QuickJoinRequest.md)
         - [Models.UpdateRequest](Models/UpdateRequest.md)
        
<a name="documentation-for-authorization"></a>
## Documentation for Authorization
    <a name="JWT"></a>
    ### JWT
        - **Type**: HTTP basic authentication
    