# How to install Unity Rooms into your Unity project

## Open The package manager

Window > Package Manager

## Install Unity Rooms

In the Package Manager Click on the "+" sign in the top left corner then select "Add Package From Disk" then browse to
`com.unity.services.rooms` and select package.json > open


## To install the Identity SDK
Modify the Unity projects manifest to contain:
```json
"dependencies": {
    "com.unity.services.identity": "0.2.1-preview"
},
"scopedRegistries": [
    {
        "name": "Internal Candidates Registry",
        "url": "https://artifactory.prd.it.unity3d.com/artifactory/api/npm/upm-candidates",
        "scopes": [
            "com.unity.services"
        ]
    }
]
```
To use the Identity SDK to retrieve the Player ID and Access Token:

```csharp
void Start()
{
    Identity.SignInAnonymously();
    Identity.OnSignedIn += () =>
    {
        playerId = Identity.PlayerId;
        Unity.Services.Rooms.JWT = Identity.AccessToken;
    };
    Identity.OnSignInFailed += s =>
    {
        Debug.Log(s);
    };
}
```

