using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Test.Tools;
using LobbyRelaySample;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.TestTools;

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
        private string m_workingLobbyId;
        private LobbyRelaySample.Auth.SubIdentity_Authentication m_auth;
        private bool m_didSigninComplete = false;
        private Dictionary<string, PlayerDataObject> m_mockUserData; // This is handled in the LobbyAsyncRequest calls normally, but we need to supply this for the direct Lobby API calls.
        LobbyUser m_LocalUser;
        [OneTimeSetUp]
        public void Setup()
        {
            m_auth = new LobbyRelaySample.Auth.SubIdentity_Authentication(() => { m_didSigninComplete = true; });
            m_mockUserData = new Dictionary<string, PlayerDataObject>();
            m_mockUserData.Add("DisplayName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "TestUser123"));
            m_LocalUser = new LobbyUser(true);
        }

        [UnityTearDown]
        public IEnumerator PerTestTeardown()
        {
            if (m_workingLobbyId != null)
            {   yield return AsyncTestHelper.Await(async ()=> await LobbyAsyncRequests.Instance.LeaveLobbyAsync(m_workingLobbyId));
                m_workingLobbyId = null;
            }
            yield return new WaitForSeconds(0.5f); // We need a yield anyway, so wait long enough to probably delete the lobby. There currently (6/22/2021) aren't other tests that would have issues if this took longer.
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            m_auth?.Dispose();
        }

        /// <summary>
        /// Make sure the entire roundtrip for Lobby works: Once signed in, create a lobby, query to make sure it exists, then delete it.
        /// </summary>
        [UnityTest]
        public IEnumerator DoRoundtrip()
        {
            #region Setup

            // Wait a reasonable amount of time for sign-in to complete.
            if (!m_didSigninComplete)
                yield return new WaitForSeconds(3);
            if (!m_didSigninComplete)
                Assert.Fail("Did not sign in.");

            // Since we're signed in through the same pathway as the actual game, the list of lobbies will include any that have been made in the game itself, so we should account for those.
            // If you want to get around this, consider having a secondary project using the same assets with its own credentials.
            yield return
                new WaitForSeconds(
                    1); // To prevent a possible 429 with the upcoming Query request, in case a previous test had one; Query requests can only occur at a rate of 1 per second.
            QueryResponse queryResponse = null;
            Debug.Log("Getting Lobby List 1");

            yield return AsyncTestHelper.Await(async () => queryResponse = await LobbyAsyncRequests.Instance.RetrieveLobbyListAsync());


            Assert.IsNotNull(queryResponse, "QueryAllLobbiesAsync should return a non-null result. (#0)");
            int numLobbiesIni = queryResponse.Results?.Count ?? 0;

            #endregion

            // Create a test lobby.
            Lobby createResponse = null;
            string lobbyName = "TestLobby-JustATest-123";

            yield return AsyncTestHelper.Await(async () =>
                createResponse = await LobbyAsyncRequests.Instance.CreateLobbyAsync(
                    lobbyName,
                    100,
                    false,
                    m_LocalUser));

            Assert.IsNotNull(createResponse, "CreateLobbyAsync should return a non-null result.");
            m_workingLobbyId = createResponse.Id;
            Assert.AreEqual(lobbyName, createResponse.Name, "Created lobby should match the provided name.");
            // Query for the test lobby via QueryAllLobbies.
            yield return new WaitForSeconds(1); // To prevent a possible 429 with the upcoming Query request.
            Debug.Log("Getting Lobby List 2");

            yield return AsyncTestHelper.Await(async () => queryResponse = await LobbyAsyncRequests.Instance.RetrieveLobbyListAsync());

            Assert.IsNotNull(queryResponse, "QueryAllLobbiesAsync should return a non-null result. (#1)");
            Assert.AreEqual(1 + numLobbiesIni, queryResponse.Results.Count, "Queried lobbies list should contain the test lobby.");
            Assert.IsTrue(queryResponse.Results.Where(r => r.Name == lobbyName).Count() == 1, "Checking queried lobby for name.");
            Assert.IsTrue(queryResponse.Results.Where(r => r.Id == m_workingLobbyId).Count() == 1, "Checking queried lobby for ID.");


            Debug.Log("Getting current Lobby");

            Lobby currentLobby = LobbyAsyncRequests.Instance.CurrentLobby;
            Assert.IsNotNull(currentLobby, "GetLobbyAsync should return a non-null result.");
            Assert.AreEqual(lobbyName, currentLobby.Name, "Checking the lobby we got for name.");
            Assert.AreEqual(m_workingLobbyId, currentLobby.Id, "Checking the lobby we got for ID.");

            Debug.Log("Deleting current Lobby");
            // Delete the test lobby.
            yield return AsyncTestHelper.Await(async ()=> await LobbyAsyncRequests.Instance.LeaveLobbyAsync(m_workingLobbyId));

            m_workingLobbyId = null;
            Debug.Log("Getting Lobby List 3");
            yield return AsyncTestHelper.Await(async () => queryResponse = await LobbyAsyncRequests.Instance.RetrieveLobbyListAsync());


            Assert.IsNotNull(queryResponse, "QueryAllLobbiesAsync should return a non-null result. (#2)");
            Assert.AreEqual(numLobbiesIni, queryResponse.Results.Count, "Queried lobbies list should be empty.");

            // Some error messages might be asynchronous, so to reduce spillover into other tests, just wait here for a bit before proceeding.
            yield return new WaitForSeconds(3);
        }

        /// <summary>
        /// If the Lobby create call fails, we return null
        /// </summary>
        [UnityTest]
        public IEnumerator CreateFailsWithNull()
        {
            if (!m_didSigninComplete)
                yield return new WaitForSeconds(3);
            if (!m_didSigninComplete)
                Assert.Fail("Did not sign in.");

            LogAssert.ignoreFailingMessages = true; // Multiple errors will appears for the exception.
            Lobby createLobby = null;
            yield return AsyncTestHelper.Await(async () =>
                createLobby = await LobbyAsyncRequests.Instance.CreateLobbyAsync(
                    "lobby name",
                    123,
                    false,
                    m_LocalUser));


            LogAssert.ignoreFailingMessages = false;

            Assert.Null(createLobby, "The returned object will be null, so expect to need to handle it.");
        }
    }
}
