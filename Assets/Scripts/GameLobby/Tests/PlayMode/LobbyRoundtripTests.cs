using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.TestTools;
using LobbyAPIInterface = LobbyRelaySample.lobby.LobbyAPIInterface;

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

        [OneTimeSetUp]
        public void Setup()
        {
            m_auth = new LobbyRelaySample.Auth.SubIdentity_Authentication(() => { m_didSigninComplete = true; });
            m_mockUserData = new Dictionary<string, PlayerDataObject>();
            m_mockUserData.Add("DisplayName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "TestUser123"));
        }

        [UnityTearDown]
        public IEnumerator PerTestTeardown()
        {
            if (m_workingLobbyId != null)
            {   LobbyAPIInterface.DeleteLobbyAsync(m_workingLobbyId, null);
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
            yield return new WaitForSeconds(1); // To prevent a possible 429 with the upcoming Query request, in case a previous test had one; Query requests can only occur at a rate of 1 per second.
            QueryResponse queryResponse = null;
            float timeout = 5;
            LobbyAPIInterface.QueryAllLobbiesAsync(new List<QueryFilter>(), (qr) => { queryResponse = qr; });
            while (queryResponse == null && timeout > 0)
            {   yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }
            Assert.Greater(timeout, 0, "Timeout check (query #0)");
            Assert.IsNotNull(queryResponse, "QueryAllLobbiesAsync should return a non-null result. (#0)");
            int numLobbiesIni = queryResponse.Results?.Count ?? 0;
            #endregion

            // Create a test lobby.
            Lobby createResponse = null;
            timeout = 5;
            string lobbyName = "TestLobby-JustATest-123";
            LobbyAPIInterface.CreateLobbyAsync(m_auth.GetContent("id"), lobbyName, 100, false, m_mockUserData, (r) => { createResponse = r; });
            while (createResponse == null && timeout > 0)
            {   yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }
            Assert.Greater(timeout, 0, "Timeout check (create)");
            Assert.IsNotNull(createResponse, "CreateLobbyAsync should return a non-null result.");
            m_workingLobbyId = createResponse.Id;
            Assert.AreEqual(lobbyName, createResponse.Name, "Created lobby should match the provided name.");

            // Query for the test lobby via QueryAllLobbies.
            yield return new WaitForSeconds(1); // To prevent a possible 429 with the upcoming Query request.
            queryResponse = null;
            timeout = 5;
            LobbyAPIInterface.QueryAllLobbiesAsync(new List<QueryFilter>(), (qr) => { queryResponse = qr; });
            while (queryResponse == null && timeout > 0)
            {   yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }
            Assert.Greater(timeout, 0, "Timeout check (query #1)");
            Assert.IsNotNull(queryResponse, "QueryAllLobbiesAsync should return a non-null result. (#1)");
            Assert.AreEqual(1 + numLobbiesIni, queryResponse.Results.Count, "Queried lobbies list should contain the test lobby.");
            Assert.IsTrue(queryResponse.Results.Where(r => r.Name == lobbyName).Count() == 1, "Checking queried lobby for name.");
            Assert.IsTrue(queryResponse.Results.Where(r => r.Id == m_workingLobbyId).Count() == 1, "Checking queried lobby for ID.");

            // Query for solely the test lobby via GetLobby.
            Lobby getResponse = null;
            timeout = 5;
            LobbyAPIInterface.GetLobbyAsync(createResponse.Id, (r) => { getResponse = r; });
            while (getResponse == null && timeout > 0)
            {   yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }
            Assert.Greater(timeout, 0, "Timeout check (get)");
            Assert.IsNotNull(getResponse, "GetLobbyAsync should return a non-null result.");
            Assert.AreEqual(lobbyName, getResponse.Name, "Checking the lobby we got for name.");
            Assert.AreEqual(m_workingLobbyId, getResponse.Id, "Checking the lobby we got for ID.");

            // Delete the test lobby.
            bool didDeleteFinish = false;
            timeout = 5;
            LobbyAPIInterface.DeleteLobbyAsync(m_workingLobbyId, () => { didDeleteFinish = true; });
            while (timeout > 0 && !didDeleteFinish)
            {   yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }
            Assert.Greater(timeout, 0, "Timeout check (delete)");
            m_workingLobbyId = null;

            // Query to ensure the lobby is gone.
            yield return new WaitForSeconds(1); // To prevent a possible 429 with the upcoming Query request.
            QueryResponse queryResponseTwo = null;
            timeout = 5;
            LobbyAPIInterface.QueryAllLobbiesAsync(new List<QueryFilter>(), (qr) => { queryResponseTwo = qr; });
            while (queryResponseTwo == null && timeout > 0)
            {   yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }
            Assert.Greater(timeout, 0, "Timeout check (query #2)");
            Assert.IsNotNull(queryResponse, "QueryAllLobbiesAsync should return a non-null result. (#2)");
            Assert.AreEqual(numLobbiesIni, queryResponseTwo.Results.Count, "Queried lobbies list should be empty.");

            // Some error messages might be asynchronous, so to reduce spillover into other tests, just wait here for a bit before proceeding.
            yield return new WaitForSeconds(3);
        }

        /// <summary>
        /// If the Lobby create call fails, we want to ensure we call onComplete so we can act on the failure.
        /// </summary>
        [UnityTest]
        public IEnumerator OnCompletesOnFailure()
        {
            if (!m_didSigninComplete)
                yield return new WaitForSeconds(3);
            if (!m_didSigninComplete)
                Assert.Fail("Did not sign in.");

            bool? didComplete = null;
            LogAssert.ignoreFailingMessages = true; // Multiple errors will appears for the exception.
            LobbyAPIInterface.CreateLobbyAsync("ThisStringIsInvalidHere", "lobby name", 123, false, m_mockUserData, (r) => { didComplete = (r == null); });
            float timeout = 5;
            while (didComplete == null && timeout > 0)
            {   yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }
            LogAssert.ignoreFailingMessages = false;

            Assert.Greater(timeout, 0, "Timeout check");
            Assert.NotNull(didComplete, "Should have called onComplete, even if the async request failed.");
            Assert.True(didComplete, "The returned object will be null, so expect to need to handle it.");
        }
    }
}
