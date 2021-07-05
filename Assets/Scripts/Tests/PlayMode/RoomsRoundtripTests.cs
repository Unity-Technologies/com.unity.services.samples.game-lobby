using NUnit.Framework;
using System.Collections;
using System.Linq;
using Unity.Services.Rooms;
using Unity.Services.Rooms.Models;
using UnityEngine;
using UnityEngine.TestTools;
using RoomsInterface = LobbyRelaySample.Lobby.RoomsInterface;

namespace Test
{
    /// <summary>
    /// Hits the Authentication and Rooms services in order to ensure rooms can be created and deleted.
    /// The actual code accessing rooms should go through RoomsQuery.
    /// </summary>
    public class RoomsRoundtripTests
    {
        private string m_workingRoomId;
        private LobbyRelaySample.Auth.SubIdentity_Authentication m_auth;
        private bool m_didSigninComplete = false;

        [OneTimeSetUp]
        public void Setup()
        {
            m_auth = new LobbyRelaySample.Auth.SubIdentity_Authentication(() => { m_didSigninComplete = true; });
        }

        [UnityTearDown]
        public IEnumerator PerTestTeardown()
        {
            if (m_workingRoomId != null)
            {   RoomsInterface.DeleteRoomAsync(m_workingRoomId, null);
                m_workingRoomId = null;
            }
            yield return new WaitForSeconds(0.5f); // We need a yield anyway, so wait long enough to probably delete the room. There currently (6/22/2021) aren't other tests that would have issues if this took longer.
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            m_auth?.Dispose();
            LogAssert.ignoreFailingMessages = false;
        }

        [UnityTest]
        public IEnumerator DoRoundtrip()
        {
            LogAssert.ignoreFailingMessages = true; // Not sure why, but when auth logs in, it sometimes generates an error: "A Native Collection has not been disposed[...]." We don't want this to cause test failures, since in practice it *seems* to not negatively impact behavior.

            // Wait a reasonable amount of time for sign-in to complete.
            if (!m_didSigninComplete)
                yield return new WaitForSeconds(3);
            if (!m_didSigninComplete)
                Assert.Fail("Did not sign in.");

            // Since we're signed in through the same pathway as the actual game, the list of rooms will include any that have been made in the game itself, so we should account for those.
            // If you want to get around this, consider having a secondary project using the same assets with its own credentials.
            yield return new WaitForSeconds(1); // To prevent a possible 429 with the upcoming Query request, in case a previous test had one; Query requests can only occur at a rate of 1 per second.
            Response<QueryResponse> queryResponse = null;
            float timeout = 5;
            RoomsInterface.QueryAllRoomsAsync((qr) => { queryResponse = qr; });
            while (queryResponse == null && timeout > 0)
            {   yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }
            Assert.Greater(timeout, 0, "Timeout check (query #0)");
            Assert.IsTrue(queryResponse.Status >= 200 && queryResponse.Status < 300, "QueryAllRoomsAsync should return a success code. (#0)");
            int numRoomsIni = queryResponse.Result.Results?.Count ?? 0;

            // Create a test room.
            Response<Room> createResponse = null;
            timeout = 5;
            string roomName = "TestRoom-JustATestRoom-123";
            RoomsInterface.CreateRoomAsync(m_auth.GetContent("id"), roomName, 100, false, (r) => { createResponse = r; });
            while (createResponse == null && timeout > 0)
            {   yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }
            Assert.Greater(timeout, 0, "Timeout check (create)");
            Assert.IsTrue(createResponse.Status >= 200 && createResponse.Status < 300, "CreateRoomAsync should return a success code.");
            m_workingRoomId = createResponse.Result.Id;
            Assert.AreEqual(roomName, createResponse.Result.Name, "Created room should match the provided name.");

            // Query for the test room via QueryAllRooms.
            yield return new WaitForSeconds(1); // To prevent a possible 429 with the upcoming Query request.
            queryResponse = null;
            timeout = 5;
            RoomsInterface.QueryAllRoomsAsync((qr) => { queryResponse = qr; });
            while (queryResponse == null && timeout > 0)
            {   yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }
            Assert.Greater(timeout, 0, "Timeout check (query #1)");
            Assert.IsTrue(queryResponse.Status >= 200 && queryResponse.Status < 300, "QueryAllRoomsAsync should return a success code. (#1)");
            Assert.AreEqual(1 + numRoomsIni, queryResponse.Result.Results.Count, "Queried rooms list should contain the test room.");
            Assert.IsTrue(queryResponse.Result.Results.Where(r => r.Name == roomName).Count() == 1, "Checking queried room for name.");
            Assert.IsTrue(queryResponse.Result.Results.Where(r => r.Id == m_workingRoomId).Count() == 1, "Checking queried room for ID.");

            // Query for solely the test room via GetRoom.
            Response<Room> getResponse = null;
            timeout = 5;
            RoomsInterface.GetRoomAsync(createResponse.Result.Id, (r) => { getResponse = r; });
            while (getResponse == null && timeout > 0)
            {   yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }
            Assert.Greater(timeout, 0, "Timeout check (get)");
            Assert.IsTrue(getResponse.Status >= 200 && getResponse.Status < 300, "GetRoomAsync should return a success code.");
            Assert.AreEqual(roomName, getResponse.Result.Name, "Checking the room we got for name.");
            Assert.AreEqual(m_workingRoomId, getResponse.Result.Id, "Checking the room we got for ID.");

            // Delete the test room.
            Response deleteResponse = null;
            timeout = 5;
            RoomsInterface.DeleteRoomAsync(m_workingRoomId, (r) => { deleteResponse = r; });
            while (deleteResponse == null && timeout > 0)
            {   yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }
            Assert.Greater(timeout, 0, "Timeout check (delete)");
            Assert.IsTrue(deleteResponse.Status >= 200 && deleteResponse.Status < 300, "DeleteRoomAsync should return a success code.");
            m_workingRoomId = null;

            // Query to ensure the room is gone.
            yield return new WaitForSeconds(1); // To prevent a possible 429 with the upcoming Query request.
            Response<QueryResponse> queryResponseTwo = null;
            timeout = 5;
            RoomsInterface.QueryAllRoomsAsync((qr) => { queryResponseTwo = qr; });
            while (queryResponseTwo == null && timeout > 0)
            {   yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }
            Assert.Greater(timeout, 0, "Timeout check (query #2)");
            Assert.IsTrue(queryResponseTwo.Status >= 200 && queryResponseTwo.Status < 300, "QueryAllRoomsAsync should return a success code. (#2)");
            Assert.AreEqual(numRoomsIni, queryResponseTwo.Result.Results.Count, "Queried rooms list should be empty.");

            // Some error messages might be asynchronous, so to reduce spillover into other tests, just wait here for a bit before proceeding.
            yield return new WaitForSeconds(3);
            LogAssert.ignoreFailingMessages = false;
        }

        [UnityTest]
        public IEnumerator OnCompletesOnFailure()
        {
            LogAssert.ignoreFailingMessages = true;
            if (!m_didSigninComplete)
                yield return new WaitForSeconds(3);
            if (!m_didSigninComplete)
                Assert.Fail("Did not sign in.");

            bool? didComplete = null;
            RoomsInterface.CreateRoomAsync("ThisStringIsInvalidHere", "room name", 123, false, (r) => { didComplete = (r == null); });
            float timeout = 5;
            while (didComplete == null && timeout > 0)
            {   yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }
            Assert.Greater(timeout, 0, "Timeout check");
            Assert.NotNull(didComplete, "Should have called onComplete, even if the async request failed.");
            Assert.True(didComplete, "The returned object will be null, so expect to need to handle it.");
        }
    }
}
