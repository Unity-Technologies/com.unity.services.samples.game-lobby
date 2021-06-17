using System.Collections;
using LobbyRooms;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Utilities;

namespace Test
{
    public class ReadyCheckTests
    {
        LobbyData m_LobbyData;
        LobbyReadyCheck m_ReadyCheck;
        const int k_TestUserCount = 3;

        [SetUp]
        public void FirstTimeSetup()
        {
            m_LobbyData = new LobbyData();
            new GameObject("SlowUpdater").AddComponent<UpdateSlow>();

            for (int i = 0; i < k_TestUserCount; i++)
            {
                m_LobbyData.AddPlayer(new LobbyUser
                {
                    ID = i.ToString()
                });
            }
        }

        [UnityTest]
        public IEnumerator TimeOutWithSingleReadyUp()
        {
            m_ReadyCheck = new LobbyReadyCheck(m_LobbyData, AssertNotReady, 1);
            yield return new WaitForSeconds(0.1f);
            m_LobbyData.LobbyUsers["0"].UserStatus = UserStatus.Ready;
            while (!m_ReadyCheck.ReadyCheckFinished)
                yield return null;
        }

        [UnityTest]
        public IEnumerator SucceedAllReadyUp()
        {
            m_ReadyCheck = new LobbyReadyCheck(m_LobbyData, AssertReady, 1);
            yield return new WaitForSeconds(0.1f);
            m_LobbyData.LobbyUsers["0"].UserStatus = UserStatus.Ready;
            yield return new WaitForSeconds(0.1f);
            m_LobbyData.LobbyUsers["1"].UserStatus = UserStatus.Ready;
            yield return new WaitForSeconds(0.1f);
            m_LobbyData.LobbyUsers["2"].UserStatus = UserStatus.Ready;
            while (!m_ReadyCheck.ReadyCheckFinished)
                yield return null;
        }

        [UnityTest]
        public IEnumerator FailOnCancel()
        {
            m_ReadyCheck = new LobbyReadyCheck(m_LobbyData, AssertNotReady, 1);
            yield return new WaitForSeconds(0.1f);
            m_LobbyData.LobbyUsers["0"].UserStatus = UserStatus.Ready;
            yield return new WaitForSeconds(0.1f);
            m_LobbyData.LobbyUsers["1"].UserStatus = UserStatus.Ready;
            yield return new WaitForSeconds(0.1f);
            m_LobbyData.LobbyUsers["2"].UserStatus = UserStatus.Cancelled;
            while (!m_ReadyCheck.ReadyCheckFinished)
                yield return null;
        }

        void AssertReady(bool ready)
        {
            Assert.IsTrue(ready, $"ReadyCheck : {ready}");
        }

        void AssertNotReady(bool ready)
        {
            Assert.False(ready, $"NotReadyCheck : {ready}");
        }
    }
}
