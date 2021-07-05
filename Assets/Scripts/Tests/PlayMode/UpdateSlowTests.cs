using LobbyRelaySample;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;

namespace Test
{ 
    public class UpdateSlowTests
    {
        private GameObject m_updateSlowObj;
        private List<Subscriber> m_activeSubscribers = new List<Subscriber>(); // For cleaning up, in case an Assert prevents a Subscriber from taking care of itself.
        private const float k_period = 1.5f;

        private class Subscriber : IDisposable
        {
            private Action m_thingToDo;

            public Subscriber(Action thingToDo)
            {
                Locator.Get.UpdateSlow.Subscribe(OnUpdate);
                m_thingToDo = thingToDo;
            }

            public void Dispose()
            {
                Locator.Get.UpdateSlow.Unsubscribe(OnUpdate);
            }

            private void OnUpdate(float dt)
            {
                m_thingToDo?.Invoke();
            }
        }

        [OneTimeSetUp]
        public void Setup()
        {
            m_updateSlowObj = new GameObject("UpdateSlowTest");
            m_updateSlowObj.AddComponent<UpdateSlow>();
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            GameObject.Destroy(m_updateSlowObj);
        }

        [UnityTearDown]
        public IEnumerator PerTestTeardown()
        {
            foreach (Subscriber sub in m_activeSubscribers)
                sub.Dispose();
            m_activeSubscribers.Clear();
            yield break;
        }

        [UnityTest]
        public IEnumerator BasicBehavior()
        {
            int updateCount = 0;
            Subscriber sub = new Subscriber(() => { updateCount++; });
            m_activeSubscribers.Add(sub);

            yield return null;
            Assert.AreEqual(0, updateCount, "Update loop should be slow and not update per-frame.");

            yield return new WaitForSeconds(k_period - 0.1f);
            Assert.AreEqual(0, updateCount, "Assuming a default period of 1.5s and a reasonable frame rate, the slow update should still not have hit.");

            yield return new WaitForSeconds(0.1f);
            Assert.AreEqual(1, updateCount, "Slow update period should have passed.");

            yield return new WaitForSeconds(k_period);
            Assert.AreEqual(2, updateCount, "Did the slow update again.");

            Subscriber sub2 = new Subscriber(() => { updateCount += 7; });
            m_activeSubscribers.Add(sub2);
            yield return new WaitForSeconds(k_period);
            Assert.AreEqual(10, updateCount, "There are two subscribers now.");

            sub2.Dispose();
            yield return new WaitForSeconds(k_period);
            Assert.AreEqual(11, updateCount, "Should have unsubscribed just the one subscriber.");

            sub.Dispose();
            yield return new WaitForSeconds(k_period);
            Assert.AreEqual(11, updateCount, "Should have unsubscribed the remaining subscriber.");
        }

        [UnityTest]
        public IEnumerator HandleLambda()
        {
            int updateCount = 0;
            Locator.Get.UpdateSlow.Subscribe((dt) => { updateCount++; });
            LogAssert.Expect(LogType.Error, new Regex(".*Removed anonymous.*"));
            yield return new WaitForSeconds(k_period + 0.1f);
            Assert.AreEqual(0, updateCount, "Lambdas should not be permitted, since they can't be Unsubscribed.");
        }

        [UnityTest]
        public IEnumerator StaggerClients()
        {
            int updateCountA = 0, updateCountB = 0;
            Subscriber subA = new Subscriber(() => { updateCountA++; });
            Subscriber subB = new Subscriber(() => { updateCountB++; });
            m_activeSubscribers.Add(subA);
            m_activeSubscribers.Add(subB);
            float periodHalf = k_period / 2;

            yield return new WaitForSeconds(periodHalf - 0.1f);
            Assert.AreEqual(0, updateCountA, "Base case (count A)");
            Assert.AreEqual(0, updateCountB, "Base case (count B)");
            
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(1, updateCountA, "Updates are now on half the normal period. First update is first.");
            Assert.AreEqual(0, updateCountB, "Updates are now on half the normal period. Second update is second.");

            yield return new WaitForSeconds(periodHalf);
            Assert.AreEqual(1, updateCountA, "First update is still offset.");
            Assert.AreEqual(1, updateCountB, "Second update should hit now.");

            subB.Dispose();
            yield return new WaitForSeconds(periodHalf);
            Assert.AreEqual(1, updateCountA, "First update should no longer be offset.");
            Assert.AreEqual(1, updateCountB, "Second update is unsubscribed.");

            yield return new WaitForSeconds(periodHalf);
            Assert.AreEqual(2, updateCountA, "First update should hit with normal timing.");
            Assert.AreEqual(1, updateCountB, "Second update is still unsubscribed.");
        }

        [UnityTest]
        public IEnumerator WhatIfASubscriberIsVerySlow()
        {
            int updateCount = 0;
            string inefficientString = "";
            Subscriber sub = new Subscriber(() => 
            {   for (int n = 0; n < 12345; n++)
                    inefficientString += n.ToString();
                updateCount++;
            });
            m_activeSubscribers.Add(sub);

            LogAssert.Expect(LogType.Error, new Regex(".*took too long.*"));
            yield return new WaitForSeconds(k_period + 0.1f);
            Assert.AreEqual(1, updateCount, "Executed the slow update.");

            yield return new WaitForSeconds(k_period);
            Assert.AreEqual(1, updateCount, "Should have removed the offending subscriber.");
        }
    }
}