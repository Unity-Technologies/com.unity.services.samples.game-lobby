
// TODO: Obsolete. Replace with something accurate to Relay with transport.



//using LobbyRelaySample;
//using NUnit.Framework;
//using System.Collections;
//using Unity.Services.Lobbies;
//using Unity.Services.Lobbies.Models;
//using UnityEngine;
//using UnityEngine.TestTools;
//using LobbyAPIInterface = LobbyRelaySample.lobby.LobbyAPIInterface;

//namespace Test
//{
//    public class LobbyReadyCheckTests
//    {
//        private string m_workingLobbyId;
//        private LobbyRelaySample.Auth.Identity m_auth;
//        private bool m_didSigninComplete = false;
//        private GameObject m_updateSlowObj;

//        [OneTimeSetUp]
//        public void Setup()
//        {
//            m_auth = new LobbyRelaySample.Auth.Identity(() => { m_didSigninComplete = true; });
//            Locator.Get.Provide(m_auth);
//            m_updateSlowObj = new GameObject("UpdateSlowTest");
//            m_updateSlowObj.AddComponent<UpdateSlow>();
//        }

//        [UnityTearDown]
//        public IEnumerator PerTestTeardown()
//        {
//            if (m_workingLobbyId != null)
//            {   LobbyAPIInterface.DeleteLobbyAsync(m_workingLobbyId, null);
//                m_workingLobbyId = null;
//            }
//            yield return new WaitForSeconds(0.5f); // We need a yield anyway, so wait long enough to probably delete the lobby. There currently (6/22/2021) aren't other tests that would have issues if this took longer.
//        }

//        [OneTimeTearDown]
//        public void Teardown()
//        {
//            Locator.Get.Provide(new LobbyRelaySample.Auth.IdentityNoop());
//            m_auth.Dispose();
//            LogAssert.ignoreFailingMessages = false;
//            LobbyAsyncRequests.Instance.EndTracking();
//            GameObject.Destroy(m_updateSlowObj);
//        }

//        private IEnumerator WaitForSignin()
//        {
//            // Wait a reasonable amount of time for sign-in to complete.
//            if (!m_didSigninComplete)
//                yield return new WaitForSeconds(3);
//            if (!m_didSigninComplete)
//                Assert.Fail("Did not sign in.");
//        }

//        private IEnumerator CreateLobby(string lobbyName, string userId)
//        {
//            Response<Lobby> createResponse = null;
//            float timeout = 5;
//            LobbyAPIInterface.CreateLobbyAsync(userId, lobbyName, 4, false, (r) => { createResponse = r; });
//            while (createResponse == null && timeout > 0)
//            {   yield return new WaitForSeconds(0.25f);
//                timeout -= 0.25f;
//            }
//            Assert.Greater(timeout, 0, "Timeout check (lobby creation).");
//            m_workingLobbyId = createResponse.Result.Id;
//        }

//        private IEnumerator PushPlayerData(LobbyUser player)
//        {
//            bool hasPushedPlayerData = false;
//            float timeout = 5;
//            LobbyAsyncRequests.Instance.UpdatePlayerDataAsync(LobbyRelaySample.lobby.ToLocalLobby.RetrieveUserData(player), () => { hasPushedPlayerData = true; }); // LobbyContentHeartbeat normally does this.
//            while (!hasPushedPlayerData && timeout > 0)
//            {   yield return new WaitForSeconds(0.25f);
//                timeout -= 0.25f;
//            }
//            Assert.Greater(timeout, 0, "Timeout check (push player data).");
//        }

//        /// <summary>
//        /// After creating a lobby and a player, signal that the player is Ready. This should lead to a countdown time being set for all players.
//        /// </summary>
//        [UnityTest]
//        public IEnumerator SetCountdownTimeSinglePlayer()
//        {
//            LogAssert.ignoreFailingMessages = true; // Not sure why, but when auth logs in, it sometimes generates an error: "A Native Collection has not been disposed[...]." We don't want this to cause test failures, since in practice it *seems* to not negatively impact behavior.
//            ReadyCheck readyCheck = new ReadyCheck(5); // This ready time is used for the countdown target end, not for any of the timing of actually detecting readies.
//            yield return WaitForSignin();

//            string userId = m_auth.GetSubIdentity(LobbyRelaySample.Auth.IIdentityType.Auth).GetContent("id");
//            yield return CreateLobby("TestReadyLobby1", userId);

//            LobbyAsyncRequests.Instance.BeginTracking(m_workingLobbyId);
//            yield return new WaitForSeconds(2); // Allow the initial lobby retrieval.

//            LobbyUser user = new LobbyUser();
//            user.ID = userId;
//            user.UserStatus = UserStatus.Ready;
//            yield return PushPlayerData(user);

//            readyCheck.BeginCheckingForReady();
//            float timeout = 5; // Long enough for two slow updates
//            yield return new WaitForSeconds(timeout);

//            readyCheck.Dispose();
//            LobbyAsyncRequests.Instance.EndTracking();

//            yield return new WaitForSeconds(2); // Buffer to prevent a 429 on the upcoming Get, since there's a Get request on the slow upate loop when that's active.
//            Response<Lobby> getResponse = null;
//            timeout = 5;
//            LobbyAPIInterface.GetLobbyAsync(m_workingLobbyId, (r) => { getResponse = r; });
//            while (getResponse == null && timeout > 0)
//            {   yield return new WaitForSeconds(0.25f);
//                timeout -= 0.25f;
//            }
//            Assert.Greater(timeout, 0, "Timeout check (get lobby).");
//            Assert.NotNull(getResponse.Result, "Retrieved lobby successfully.");
//            Assert.NotNull(getResponse.Result.Data, "Lobby should have data.");

//            Assert.True(getResponse.Result.Data.ContainsKey("AllPlayersReady"), "Check for AllPlayersReady key.");
//            string readyString = getResponse.Result.Data["AllPlayersReady"]?.Value;
//            Assert.NotNull(readyString, "Check for non-null AllPlayersReady.");
//            Assert.True(long.TryParse(readyString, out long ticks), "Check for ticks value in AllPlayersReady."); // This will be based on the current time, so we won't check for a specific value.
//        }

//        // Can't test with multiple players on one machine, since anonymous UAS credentials can't be manually supplied.
//    }
//}
