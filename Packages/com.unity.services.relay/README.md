# Relay SDK

The Relay SDK provides methods to Create [Allocations][glossary], Create [Join Codes][glossary] and Use [Join Codes][glossary] to join a host.

Relay SDK depends on the Operate Core SDK. 

To use the SDK the player has to be authenticated, using the Authentication SDK:

## Using the Relay SDK

### Import packages

```csharp
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Allocations;
using Unity.Services.Relay.Http;
using Unity.Services.Relay.Models;
```

### Player authentication

Each player must be authenticated. The simplest way to authenticate players is through anonymous authentication using the Authentication SDK. However, other methods are also supported.

```csharp
try
{
    await UnityServices.Initialize();
    await AuthenticationService.Instance.SignInAnonymouslyAsync();
    var playerID = Authentication.PlayerId;
}
catch (Exception e)
{
    Debug.Log(e);
}
```

### Setup the host

Typically, the player who is hosting the game creates a Relay server allocation. Allocations reserve capacity on a Relay server and provide a way to address data packets.

```csharp
try
{
    var maxNumberOfPlayer = 10;
    Response<AllocateResponseBody> response = await RelayService.AllocationsApiClient.CreateAllocationAsync(new CreateAllocationRequest(new AllocationRequest(maxNumberOfPlayer)));
    var allocation = allocationTask.Result.Result.Data.Allocation;
    Debug.Log(allocation.AllocationId);
}
catch (HttpException he)
{
    Debug.Log(he);
}
```

You can use join codes to allow the host player to invite other players to join the game. The Relay can generate random join codes that are short and easy to read. The join codes are intended to be shared out-of-band, such as through a third-party chat service.

```csharp
try
{
    Response<JoinCodeResponseBody> response = await RelayService.AllocationsApiClient.CreateJoincodeAsync(new CreateJoincodeRequest(new JoinCodeRequest(hostAllocationId)));
    JoinCodeData joinCodeData = response.Result.Data;
    joinCode = joinCodeData.JoinCode;
    Debug.Log(joinCode);
    UpdateUI();
}
catch (HttpException he)
{
    Debug.Log(he);
}
```

### Setup the client

Players can join the host by using the join code. You can use the Relay SDK to connect (that is, a Relay endpoint) to the host player.

```csharp
try
{
    Response<JoinResponseBody> response = await RelayService.AllocationsApiClient.JoinRelayAsync(new JoinRelayRequest(new JoinRequest(joinCode)));
    var allocation = response.Result.Data.Allocation;
    Debug.Log(allocation.AllocationId);
}
catch (HttpException he)
{
    Debug.Log(he);
}
```

[glossary]: http://documentation.cloud.unity3d.com/en/articles/5371884-relay-glossary