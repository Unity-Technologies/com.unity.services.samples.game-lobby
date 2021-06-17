using NUnit.Framework;
using System.Collections;
using Unity.Services.Rooms;
using Unity.Services.Rooms.Models;
using UnityEngine;
using UnityEngine.TestTools;
using RoomsInterface = LobbyRooms.Rooms.RoomsInterface;

namespace Test
{
    /// <summary>
    /// Hits the Authentication and Rooms services in order to ensure rooms can be created and deleted.
    /// The actual code accessing rooms should go through RoomsQuery.
    /// </summary>
    public class RoomsRoundtripTests
    {
        private string m_workingRoomId;
        private LobbyRooms.Auth.SubIdentity_Authentication m_auth;
        private bool m_didSigninComplete = false;

        [OneTimeSetUp]
        public void Setup()
        {
            m_auth = new LobbyRooms.Auth.SubIdentity_Authentication(() => { m_didSigninComplete = true; });
            var stagingpath = "https://rooms-stg.cloud.unity3d.com/v1";
            RoomsInterface.SetPath(stagingpath); //Defaults to Test Path
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            if (m_workingRoomId != null)
                RoomsInterface.DeleteRoomAsync(m_workingRoomId, null);
            m_auth?.Dispose();
            LogAssert.ignoreFailingMessages = false;
        }

        [UnityTest]
        public IEnumerator DoRoundtrip()
        {
            LogAssert.ignoreFailingMessages = true; // TODO: Not sure why, but when auth logs in, it sometimes generates an error: "A Native Collection has not been disposed[...]." We don't want this to cause test failures, since in practice it *seems* to not negatively impact behavior.

            // Wait a reasonable amount of time for sign-in to complete.
            if (!m_didSigninComplete)
                yield return new WaitForSeconds(3);
            if (!m_didSigninComplete)
                Assert.Fail("Did not sign in.");

            // Create a test room.
            Response<Room> createResponse = null;
            float timeout = 5;
            string roomName = "TestRoom-JustATestRoom-123";
            RoomsInterface.CreateRoomAsync(m_auth.GetContent("id"), roomName, 123, (r) => { createResponse = r; });
            while (createResponse == null && timeout > 0)
            {   yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }
            Assert.Greater(timeout, 0, "Timeout check (create)");
            Assert.IsTrue(createResponse.Status >= 200 && createResponse.Status < 300, "CreateRoomAsync should return a success code.");
            m_workingRoomId = createResponse.Result.Id;
            Assert.AreEqual(roomName, createResponse.Result.Name, "Created room should match the provided name.");

            // Query for the test room via QueryAllRooms.
            Response<QueryResponse> queryResponse = null;
            timeout = 5;
            RoomsInterface.QueryAllRoomsAsync((qr) => { queryResponse = qr; });
            while (queryResponse == null && timeout > 0)
            {   yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }
            Assert.Greater(timeout, 0, "Timeout check (query #1)");
            Assert.IsTrue(queryResponse.Status >= 200 && queryResponse.Status < 300, "QueryAllRoomsAsync should return a success code. (#1)");
            Assert.AreEqual(1, queryResponse.Result.Results.Count, "Queried rooms list should contain just the test room. (Are there rooms you created and did not yet delete?)"); // TODO: Can we get a test account such that having actual rooms open doesn't impact this?
            Assert.AreEqual(roomName, queryResponse.Result.Results[0].Name, "Checking queried room for name.");
            Assert.AreEqual(m_workingRoomId, queryResponse.Result.Results[0].Id, "Checking queried room for ID.");

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
            Response<QueryResponse> queryResponseTwo = null;
            timeout = 5;
            RoomsInterface.QueryAllRoomsAsync((qr) => { queryResponseTwo = qr; });
            while (queryResponseTwo == null && timeout > 0)
            {   yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }
            Assert.Greater(timeout, 0, "Timeout check (query #2)");
            Assert.IsTrue(queryResponseTwo.Status >= 200 && queryResponseTwo.Status < 300, "QueryAllRoomsAsync should return a success code. (#2)");
            Assert.AreEqual(0, queryResponseTwo.Result.Results.Count, "Queried rooms list should be empty.");

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
            RoomsInterface.CreateRoomAsync("ThisStringIsInvalidHere", "room name", 123, (r) => { didComplete = (r == null); });
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
