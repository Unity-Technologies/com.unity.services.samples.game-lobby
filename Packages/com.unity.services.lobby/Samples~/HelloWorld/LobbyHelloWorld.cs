using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Http;
using Unity.Services.Lobbies.Models;
using Random = UnityEngine.Random;

public class LobbyHelloWorld : MonoBehaviour
{
    // Inspector properties with initial values
    public string newLobbyName = Guid.NewGuid().ToString();
    public int maxPlayers = 8;
    public bool isPrivate = false;

    // We'll only be in one lobby at once for this demo, so let's track it here
    private Lobby currentLobby;

    async void Start()
    {
        try
        {
            await ExecuteLobbyDemoAsync();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
        finally
        {
            await CleanupDemoLobbyAsync();
        }
    }

    // Clean up the lobby we're in if we're the host
    async Task CleanupDemoLobbyAsync()
    {
        var localPlayerId = AuthenticationService.Instance.PlayerId;

        // This is so that orphan lobbies aren't left around in case the demo fails partway through
        if (currentLobby != null && currentLobby.HostId.Equals(localPlayerId))
        {
            await DeleteLobbyAsync(currentLobby.Id);
        }
    }

    // A basic demo of lobby functionality
    async Task ExecuteLobbyDemoAsync()
    {
        await UnityServices.Initialize();

        // Log in a player for this game client
        Player loggedInPlayer = await GetPlayerFromAnonymousLoginAsync();

        // Add some data to our player
        // This data will be included in a lobby under players -> player.data
        loggedInPlayer.Data.Add("Ready", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "No"));

        // Look for existing lobbies
        List<Lobby> foundLobbies = await QueryLobbiesAsync(new QueryRequest(
            count: 10, // Return up to 10 results
            sampleResults: false, // Do not give random results
            filter: new List<QueryFilter>
            {
                // Use filters to only return lobbies which match specific conditions
                // You can only filter on built-in properties (Ex: AvailableSlots) or indexed custom data (S1, N1, etc.)
                // Take a look at the API for other built-in fields you can filter on

                // Let's search for games with open slots (AvailableSlots greater than 0)
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0"),

                // Let's add some filters for custom indexed fields
                new QueryFilter(
                    field: QueryFilter.FieldOptions.S1, // S1 = "Test"
                    op: QueryFilter.OpOptions.EQ,
                    value: "true"),

                new QueryFilter(
                    field: QueryFilter.FieldOptions.S2, // S2 = "GameMode"
                    op: QueryFilter.OpOptions.EQ,
                    value: "ctf"),

                // Example "skill" range filter (skill is a custom numeric field in this example)
                new QueryFilter(
                    field: QueryFilter.FieldOptions.N1, // N1 = "Skill"
                    op: QueryFilter.OpOptions.GT,
                    value: "0"),

                new QueryFilter(
                    field: QueryFilter.FieldOptions.N1, // N1 = "Skill"
                    op: QueryFilter.OpOptions.LT,
                    value: "51"),
            },
            order: new List<QueryOrder>
            {
                // Order results by available player slots (least first), then by lobby age, then by lobby name
                new QueryOrder(true, QueryOrder.FieldOptions.AvailableSlots),
                new QueryOrder(false, QueryOrder.FieldOptions.Created),
                new QueryOrder(false, QueryOrder.FieldOptions.Name),
            }));

        if (foundLobbies.Any()) // Try to join a random lobby if one exists
        {
            // Let's print info about the lobbies we found
            Debug.Log("Found lobbies:\n" + JsonConvert.SerializeObject(foundLobbies));

            // Let's pick a random lobby to join
            var randomLobby = foundLobbies[Random.Range(0, foundLobbies.Count)];

            // Try to join the lobby
            // Player is optional because the service can pull the player data from the auth token
            currentLobby = await JoinLobbyAsync(lobbyId: randomLobby.Id, player: null);

            // You can also join via a Lobby Code instead of a lobby ID
            // Lobby Codes are a short, unique codes that map to a specific lobby Id
            // currentLobby = await JoinLobbyWithCodeAsync("MyLobbyCode");
        }
        else // Didn't find any lobbies, create a new lobby
        {
            // Populate the new lobby with some data; use indexes so it's easy to search for
            var lobbyData = new Dictionary<string, DataObject>()
            {
                ["Test"] = new DataObject(DataObject.VisibilityOptions.Public, "true", DataObject.IndexOptions.S1),
                ["GameMode"] = new DataObject(DataObject.VisibilityOptions.Public, "ctf", DataObject.IndexOptions.S2),
                ["Skill"] = new DataObject(DataObject.VisibilityOptions.Public, Random.Range(1, 51).ToString(), DataObject.IndexOptions.N1),
                ["Rank"] = new DataObject(DataObject.VisibilityOptions.Public, Random.Range(1, 51).ToString()),
            };

            // Create a new lobby
            currentLobby = await CreateLobbyAsync(new CreateRequest(
                name: newLobbyName,
                player: loggedInPlayer,
                maxPlayers: maxPlayers,
                isPrivate: isPrivate,
                data: lobbyData));
        }

        // Let's write a little info about the lobby we joined / created
        Debug.Log("Lobby info:\n" + JsonConvert.SerializeObject(currentLobby));

        // Let's add some new data for our player and update the lobby state
        // Players can update their own data
        loggedInPlayer.Data.Add("ExamplePublicPlayerData",
            new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, "Everyone can see this"));

        loggedInPlayer.Data.Add("ExamplePrivatePlayerData",
            new PlayerDataObject(PlayerDataObject.VisibilityOptions.Private, "Only the host sees this"));

        loggedInPlayer.Data.Add("ExampleMemberPlayerData",
            new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "Only lobby members see this"));

        // Update the lobby
        currentLobby = await UpdateLobbyPlayerDataAsync(currentLobby.Id, loggedInPlayer);
        Debug.Log("Updated lobby info:\n" + JsonConvert.SerializeObject(currentLobby));

        // Let's poll for the lobby data again just to see what it looks like
        currentLobby = await GetLobbyAsync(currentLobby.Id);
        Debug.Log("Updated lobby info:\n" + JsonConvert.SerializeObject(currentLobby));

        if (!currentLobby.HostId.Equals(loggedInPlayer.Id))
        {
            // Since we're not the lobby host, let's just leave the lobby
            await RemovePlayerFromLobbyAsync(currentLobby.Id, loggedInPlayer.Id);
        }
        else
        {
            // Only hosts can set lobby data, and we're the host, so let's set some
            // Note that lobby host can be passed around intentionally (by the current host updating the host id)
            // Host is randomly assigned if the previous host leaves

            // Let's update some existing lobby data
            currentLobby.Data["GameMode"] =
                new DataObject(DataObject.VisibilityOptions.Public, "deathmatch", DataObject.IndexOptions.S2);

            // Let's add some new data to the lobby
            currentLobby.Data.Add("ExamplePublicLobbyData",
                new DataObject(DataObject.VisibilityOptions.Public, "Everyone can see this"));

            currentLobby.Data.Add("ExamplePrivateLobbyData",
                new DataObject(DataObject.VisibilityOptions.Private, "Only the host sees this"));

            currentLobby.Data.Add("ExampleMemberLobbyData",
                new DataObject(DataObject.VisibilityOptions.Member, "Only lobby members see this"));

            // OK, now let's try to push these local changes to the service
            currentLobby = await UpdateLobbyDataAsync(currentLobby);

            // Let's print the updated lobby
            Debug.Log("Updated lobby info:\n" + JsonConvert.SerializeObject(currentLobby));

            // OK, we're done with the lobby - let's delete it
            await DeleteLobbyAsync(currentLobby.Id);
        }

        // Now, let's try the QuickJoin API, which just puts our player in a matching lobby automatically
        // This is fast and reliable, but removes some user interactivity (can't choose from a list of lobbies)
        // You can use filters to specify which types of lobbies can be joined
        currentLobby = await QuickJoinLobbyAsync(loggedInPlayer, new List<QueryFilter>
        {
            // Let's search for games with open slots (AvailableSlots greater than 0)
            new QueryFilter(
                field: QueryFilter.FieldOptions.AvailableSlots,
                op: QueryFilter.OpOptions.GT,
                value: "0"),

            // You can add more filters here, such as filters on custom data fields
        });

        // If we didn't find a lobby, abort run
        if (currentLobby == null)
        {
            Debug.Log("Unable to find a lobby using Query and Join");
            return;
        }

        // Let's write a little info about the lobby we quick-joined
        Debug.Log("Lobby info:\n" + JsonConvert.SerializeObject(currentLobby));

        // There's not anything else we can really do here, so let's leave the lobby
        await RemovePlayerFromLobbyAsync(currentLobby.Id, loggedInPlayer.Id);
    }

    // Log in a player using Unity's "Anonymous Login" API and construct a Player object for use with the Lobbies APIs
    static async Task<Player> GetPlayerFromAnonymousLoginAsync()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log($"Trying to log in a player ...");

            // Use Unity Authentication to log in
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("Player was not signed in successfully?");
                throw new InvalidOperationException("Unable to continue with no logged in player");
            }
        }

        Debug.Log("Player signed in as " + AuthenticationService.Instance.PlayerId);

        // Player objects have Get-only properties, so you need to initialize the data bag here if you want to use it
        return new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject>());
    }

    // Create a new lobby
    static async Task<Lobby> CreateLobbyAsync(CreateRequest createRequest)
    {
        Debug.Log($"Trying to create a new lobby ...");
        Response<Lobby> response = await LobbyService.LobbyApiClient.CreateLobbyAsync(new CreateLobbyRequest(createRequest));
        Debug.Log($"Player {createRequest.Player.Id} created lobby {response.Result.Id}");
        return response.Result;
    }

    // Get a list of existing lobbies (Optional: use a filter)
    static async Task<List<Lobby>> QueryLobbiesAsync(QueryRequest queryRequest)
    {
        Debug.Log($"Trying to query compatible lobbies ...");
        Response<QueryResponse> response = await LobbyService.LobbyApiClient.QueryLobbiesAsync(new QueryLobbiesRequest(queryRequest));
        Debug.Log($"Found {response.Result.Results.Count} matching lobbies");
        return response.Result.Results;
    }

    // Ask the service to join any available lobby; can provide a filter
    static async Task<Lobby> QuickJoinLobbyAsync(Player player, List<QueryFilter> filter = null)
    {
        Debug.Log($"Trying to query and join a compatible lobby ...");

        var request = new QuickJoinLobbyRequest(new QuickJoinRequest(
            player: player,
            filter: filter
        ));

        // This is an example of how to handle some common errors when calling the service
        // 404s are expected if no lobby is found, and the API currently throws exceptions on 404
        try
        {
            Response<Lobby> response = await LobbyService.LobbyApiClient.QuickJoinLobbyAsync(request);
            Debug.Log($"Player {player.Id} joined lobby {response.Result.Id}");
            return response.Result;
        }
        catch (HttpException ex)
        {
            if (TryGetErrorFromResponse(ex.Response, out var error))
            {
                // If we got a structured error we can deserialize, let's log it
                Debug.Log($"Lobby service error; Http Status {ex.Response.StatusCode}" +
                    $"\nResponse Error: {ex.Response.ErrorMessage}" +
                    $"\nLobby Error Status: {error.status}" +
                    $"\nLobby Error Title: {error.title}");
            }
            else
            {
                // For error we can't deserialize, log the exception + body for debugging
                Debug.Log($"Lobby service error; Http Status {ex.Response.StatusCode}" +
                    $"\nResponse Error: {ex.Response.ErrorMessage}" +
                    $"\nBody: {Encoding.UTF8.GetString(ex.Response.Data)}");

                Debug.LogException(ex);
            }
        }

        return null;
    }

    // Try to join a specific lobby
    static async Task<Lobby> JoinLobbyAsync(string lobbyId, Player player = null)
    {
        Debug.Log($"Attempting to join lobby {lobbyId} ...");
        Response<Lobby> response = await LobbyService.LobbyApiClient.JoinLobbyByIdAsync(
            new JoinLobbyByIdRequest(lobbyId, player));
        Debug.Log($"Joined lobby {response.Result.Id}");
        return response.Result;
    }

    // Try to join a specific lobby; use a lobbyCode to join instead of a LobbyId
    static async Task<Lobby> JoinLobbyWithCodeAsync(string lobbyCode, Player player = null)
    {
        Debug.Log($"Attempting to join lobby with lobby code {lobbyCode} ...");
        Response<Lobby> response = await LobbyService.LobbyApiClient.JoinLobbyByCodeAsync(
            new JoinLobbyByCodeRequest(new JoinByCodeRequest(lobbyCode, player)));
        Debug.Log($"Joined lobby {response.Result.Id}");
        return response.Result;
    }

    // Get details about a specific lobby
    static async Task<Lobby> GetLobbyAsync(string lobbyId)
    {
        Debug.Log($"Getting lobby {lobbyId} ...");
        Response<Lobby> response = await LobbyService.LobbyApiClient.GetLobbyAsync(
            new GetLobbyRequest(lobbyId));
        return response.Result;
    }

    // Delete an existing lobby; see API docs for access restrictions
    static async Task DeleteLobbyAsync(string lobbyId)
    {
        Debug.Log($"Deleting lobby {lobbyId} ...");
        Response response = await LobbyService.LobbyApiClient.DeleteLobbyAsync(
            new DeleteLobbyRequest(lobbyId));
        Debug.Log($"Lobby {lobbyId} deleted");
    }

    // Remove a player from a lobby; see API docs for access restrictions
    static async Task RemovePlayerFromLobbyAsync(string lobbyId, string playerId)
    {
        Debug.Log($"Removing {playerId} from lobby {lobbyId} ...");
        var request = new RemovePlayerRequest(lobbyId, playerId);
        Response response = await LobbyService.LobbyApiClient.RemovePlayerAsync(request);
        Debug.Log($"Player {playerId} removed from lobby {lobbyId}");
    }

    // Set lobby data; see API docs for access restrictions
    static async Task<Lobby> UpdateLobbyDataAsync(Lobby lobby)
    {
        Debug.Log($"Updating lobby data for lobby {lobby.Id} ...");

        UpdateRequest updatedState = new UpdateRequest(
            maxPlayers: lobby.MaxPlayers,
            isPrivate: lobby.IsPrivate,
            data: lobby.Data,
            hostId: lobby.HostId,
            name: lobby.Name);

        var updateReq = new UpdateLobbyRequest(lobby.Id, updatedState);
        Response<Lobby> response = await LobbyService.LobbyApiClient.UpdateLobbyAsync(updateReq);
        Debug.Log($"Lobby data for lobby {lobby.Id} updated");
        return response.Result;
    }

    // Set player data in a lobby; see API docs for access restrictions
    static async Task<Lobby> UpdateLobbyPlayerDataAsync(string lobbyId, Player player)
    {
        Debug.Log($"Updating player data for player {player.Id} in lobby {lobbyId} ...");

        var newPlayerState = new PlayerUpdateRequest(
            connectionInfo: player.ConnectionInfo,
            data: player.Data,
            allocationId: player.AllocationId);

        var updateReq = new UpdatePlayerRequest(lobbyId, player.Id, newPlayerState);

        Response<Lobby> response = await LobbyService.LobbyApiClient.UpdatePlayerAsync(updateReq);
        Debug.Log($"Player data for player {player.Id} in lobby {lobbyId} updated");
        return response.Result;
    }

    // A class for errors returned from the service on failures (4xx errors, etc.)
    class LobbyError
    {
        public int status { get; set; }
        public string title { get; set; }
        public Detail[] details { get; set; }
    }

    class Detail
    {
        public string message { get; set; }
        public string errorType { get; set; }
    }

    static bool TryGetErrorFromResponse(HttpClientResponse response, out LobbyError error)
    {
        var decodedJsonString = Encoding.UTF8.GetString(response.Data);

        var success = true;

        var settings = new JsonSerializerSettings
        {
            Error = (sender, args) =>
            {
                success = false;
                args.ErrorContext.Handled = true;
            },
            MissingMemberHandling = Newtonsoft.Json.MissingMemberHandling.Ignore
        };

        error = JsonConvert.DeserializeObject<LobbyError>(decodedJsonString, settings);
        return success;
    }
}
