# Unity Lobby

## Installing Unity Lobby

1. Launch Unity
2. Window > Package Manager
3. In the Package Manager Click on the "+" sign in the top left corner then select "Add Package From Disk" then browse to
`com.unity.services.lobby` and select package.json > open

## Unity Authentication Requirement

The Unity Lobby service requires Unity authentication.  To use it, install the `com.unity.services.authentication` package.

### Using Unity Authentication

To use authentication, you will need to import the package:

```csharp
using Unity.Services.Authentication;
```

Once imported, you will need to log in before using API calls.

Sample Usage:

```csharp
async void Start()
{
    await UnityServices.Initialize();
    await AuthenticationService.Instance.SignInAnonymouslyAsync();
    if (AuthenticationService.Instance.IsSignedIn)
    {
        MakeAPICall();
    }
    else
    {
        Debug.Log("Player was not signed in successfully?");
    }

}

async void MakeAPICall()
{
    FakeApiGetRequest r = new FakeApiGetRequest("fakeParameter");
    var response = await LobbyApiService.FakeClient.FakeApiGetAsync(r);
}
```
