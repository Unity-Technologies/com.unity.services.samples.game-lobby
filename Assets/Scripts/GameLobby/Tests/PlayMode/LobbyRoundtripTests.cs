using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Test.Tools;
using LobbyRelaySample;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace Test
{
    /// <summary>
    /// Accesses the Authentication and Lobby services in order to ensure lobbies can be created and deleted.
    /// LobbyAsyncRequests wraps the Lobby API, so go through that in practice. This simply ensures the connection to the Lobby service is functional.
    ///
    /// If the tests pass, you can assume you are connecting to the Lobby service itself properly.
    /// </summary>
    public class LobbyRoundtripTests
    {
        string playerID;

        Dictionary<string, PlayerDataObject>
            m_mockUserData; // This is handled in the LobbyAsyncRequest calls normally, but we need to supply this for the direct Lobby API calls.

        LocalPlayer m_LocalUser;
        LobbyManager m_LobbyManager;

        [OneTimeSetUp]
        public void Setup()
        {
            m_mockUserData = new Dictionary<string, PlayerDataObject>();
            m_mockUserData.Add("DisplayName",
                new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "TestUser123"));


#pragma warning disable 4014
            TestAuthSetup();
#pragma warning restore 4014
            m_LocalUser = new LocalPlayer(Auth.ID(), 0, false, "TESTPLAYER");
            m_LobbyManager = new LobbyManager();
        }

        async Task TestAuthSetup()
        {
            await Auth.Authenticate("test");
        }

        [UnityTearDown]
        public IEnumerator PerTestTeardown()
        {
            if (m_LobbyManager.CurrentLobby != null)
            {
                yield return AsyncTestHelper.Await(async () => await m_LobbyManager.LeaveLobbyAsync());
            }
        }

        /// <summary>
        /// Make sure the entire roundtrip for Lobby works: Once signed in, create a lobby, query to make sure it exists, then delete it.
        /// </summary>
        [UnityTest]
        public IEnumerator DoRoundtrip()
        {
            #region Setup

            yield return AsyncTestHelper.Await(async () => await Auth.Authenticating());

            // Since we're signed in through the same pathway as the actual game, the list of lobbies will include any that have been made in the game itself, so we should account for those.
            // If you want to get around this, consider having a secondary project using the same assets with its own credentials.
            yield return
                new WaitForSeconds(
                    1); // To prevent a possible 429 with the upcoming Query request, in case a previous test had one; Query requests can only occur at a rate of 1 per second.
            QueryResponse queryResponse = null;
            Debug.Log("Getting Lobby List 1");

            yield return AsyncTestHelper.Await(
                async () => queryResponse = await m_LobbyManager.RetrieveLobbyListAsync());

            Assert.IsNotNull(queryResponse, "QueryAllLobbiesAsync should return a non-null result. (#0)");
            int numLobbiesIni = queryResponse.Results?.Count ?? 0;

            #endregion

            // Create a test lobby.
            Lobby createLobby = null;
            string lobbyName = "TestLobby-JustATest-123";

            yield return AsyncTestHelper.Await(async () =>
                createLobby = await m_LobbyManager.CreateLobbyAsync(
                    lobbyName,
                    100,
                    false,
                    m_LocalUser));

            Assert.IsNotNull(createLobby, "CreateLobbyAsync should return a non-null result.");
            Assert.AreEqual(lobbyName, createLobby.Name, "Created lobby should match the provided name.");
            var createLobbyId = createLobby.Id;

            // Query for the test lobby via QueryAllLobbies.
            Debug.Log("Getting Lobby List 2");

            yield return AsyncTestHelper.Await(
                async () => queryResponse = await m_LobbyManager.RetrieveLobbyListAsync());

            Assert.IsNotNull(queryResponse, "QueryAllLobbiesAsync should return a non-null result. (#1)");
            Assert.AreEqual(1 + numLobbiesIni, queryResponse.Results.Count,
                "Queried lobbies list should contain the test lobby.");
            Assert.IsTrue(queryResponse.Results.Where(r => r.Name == lobbyName).Count() == 1,
                "Checking queried lobby for name.");
            Assert.IsTrue(queryResponse.Results.Where(r => r.Id == createLobbyId).Count() == 1,
                "Checking queried lobby for ID.");

            Debug.Log("Getting current Lobby");

            Lobby currentLobby = m_LobbyManager.CurrentLobby;
            Assert.IsNotNull(currentLobby, "GetLobbyAsync should return a non-null result.");
            Assert.AreEqual(lobbyName, currentLobby.Name, "Checking the lobby we got for name.");
            Assert.AreEqual(createLobbyId, currentLobby.Id, "Checking the lobby we got for ID.");

            Debug.Log("Deleting current Lobby");

            // Delete the test lobby.
            yield return AsyncTestHelper.Await(async () => await m_LobbyManager.LeaveLobbyAsync());

            createLobbyId = null;
            Debug.Log("Getting Lobby List 3");
            yield return AsyncTestHelper.Await(
                async () => queryResponse = await m_LobbyManager.RetrieveLobbyListAsync());

            Assert.IsNotNull(queryResponse, "QueryAllLobbiesAsync should return a non-null result. (#2)");
            Assert.AreEqual(numLobbiesIni, queryResponse.Results.Count, "Queried lobbies list should be empty.");
        }

        /// <summary>
        /// If the Lobby create call fails, we return null
        /// </summary>
        [UnityTest]
        public IEnumerator CreateFailsWithNull()
        {
            yield return AsyncTestHelper.Await(async () => await Auth.Authenticating());

            LogAssert.ignoreFailingMessages = true; // Multiple errors will appears for the exception.
            Lobby createLobby = null;
            yield return AsyncTestHelper.Await(async () =>
                createLobby = await m_LobbyManager.CreateLobbyAsync(
                    "lobby name",
                    123,
                    false,
                    m_LocalUser));

            LogAssert.ignoreFailingMessages = false;
            Assert.Null(createLobby, "The returned object will be null, so expect to need to handle it.");
            yield return
                new WaitForSeconds(
                    3); //Since CreateLobby cannot be queued, we need to give this a buffer before moving on to other tests.
        }

        [UnityTest]
        public IEnumerator CooldownTest()
        {
            var rateLimiter = new RateLimiter(3);
            Stopwatch timer = new Stopwatch();
            timer.Start();

            //pass Through the first request, which triggers the cooldown.
            yield return AsyncTestHelper.Await(async () => await rateLimiter.WaitUntilCooldown());

            //Should wait for one second total
            yield return AsyncTestHelper.Await(async () => await rateLimiter.WaitUntilCooldown());
            timer.Stop();
            var elapsedMS = timer.ElapsedMilliseconds;
            Debug.Log($"Cooldown took {elapsedMS}/{rateLimiter.coolDownMS} milliseconds.");
            var difference = Mathf.Abs(elapsedMS - rateLimiter.coolDownMS);
            Assert.IsTrue(difference < 50 && difference >= 0);
        }
    }
}
