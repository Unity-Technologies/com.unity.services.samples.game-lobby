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
    /// <summary>
    /// Testing some edge cases with the UpdateSlow.
    /// </summary>
    public class UpdateSlowTests
    {
        private GameObject m_updateSlowObj;
        private List<Subscriber> m_activeSubscribers = new List<Subscriber>(); // For cleaning up, in case an Assert prevents a Subscriber from taking care of itself.

        /// <summary>Trivial Subscriber to do some action every UpdateSlow.</summary>
        private class Subscriber : IDisposable
        {
            private Action m_thingToDo;
            public float prevDt;

            public Subscriber(Action thingToDo, float period)
            {
                Locator.Get.UpdateSlow.Subscribe(OnUpdate, period);
                m_thingToDo = thingToDo;
            }

            public void Dispose()
            {
                Locator.Get.UpdateSlow.Unsubscribe(OnUpdate);
            }

            private void OnUpdate(float dt)
            {
                m_thingToDo?.Invoke();
                prevDt = dt;
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
        public IEnumerator BasicBehavior_MultipleSubs()
        {
            int updateCount = 0;
            float period = 1.5f;
            Subscriber sub = new Subscriber(() => { updateCount++; }, period);
            m_activeSubscribers.Add(sub);

            yield return null;
            Assert.AreEqual(0, updateCount, "Update loop should be slow and not update per-frame.");

            yield return new WaitForSeconds(period - 0.1f);
            Assert.AreEqual(0, updateCount, "Assuming a default period of 1.5s and a reasonable frame rate, the slow update should still not have hit.");

            yield return new WaitForSeconds(0.1f);
            Assert.AreEqual(1, updateCount, "Slow update period should have passed.");
            Assert.AreNotEqual(period, sub.prevDt, "Slow update should have received the actual amount of time that passed, not necessarily its period.");
            Assert.True(sub.prevDt - period < 0.05f && sub.prevDt - period > 0, "The time delta received by slow update should match the actual time since their previous update.");

            yield return new WaitForSeconds(period);
            Assert.AreEqual(2, updateCount, "Did the slow update again.");
            Assert.AreNotEqual(period, sub.prevDt, "Slow update should have received the full time delta, not just its period, again.");
            Assert.True(sub.prevDt - period < 0.05f && sub.prevDt - period > 0, "The time delta received by slow update should match the actual time since their previous update, again.");

            float period2 = period - 0.2f;
            Subscriber sub2 = new Subscriber(() => { updateCount += 7; }, period2);
            m_activeSubscribers.Add(sub2);
            yield return new WaitForSeconds(period);
            Assert.AreEqual(10, updateCount, "There are two subscribers now.");
            Assert.True(sub.prevDt - period < 0.05f && sub.prevDt - period > 0, "Slow update on the first subscriber should have received the full time delta with two subscribers.");
            Assert.True(sub2.prevDt - period2 < 0.05f && sub2.prevDt - period2 > 0, "Slow update on the second subscriber should receive the actual time, even if its period is shorter.");

            sub2.Dispose();
            yield return new WaitForSeconds(period);
            Assert.AreEqual(11, updateCount, "Should have unsubscribed just the one subscriber.");

            sub.Dispose();
            yield return new WaitForSeconds(period);
            Assert.AreEqual(11, updateCount, "Should have unsubscribed the remaining subscriber.");
        }

        [UnityTest]
        public IEnumerator BasicBehavior_UpdateEveryFrame()
        {
            int updateCount = 0;
            Subscriber sub = new Subscriber(() => { updateCount++; }, 0);
            m_activeSubscribers.Add(sub);

            yield return null;
            Assert.AreEqual(1, updateCount, "Update loop should update per-frame if a subscriber opts for that (#1).");
            yield return null;
            Assert.AreEqual(2, updateCount, "Update loop should update per-frame if a subscriber opts for that (#2).");
            Assert.AreEqual(sub.prevDt, Time.deltaTime, "Subscriber should receive the correct update time since their previous update.");

            sub.Dispose();
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(2, updateCount, "Should have unsubscribed the subscriber.");
        }

        [UnityTest]
        public IEnumerator HandleLambda()
        {
            int updateCount = 0;
            float period = 0.5f;
            Locator.Get.UpdateSlow.Subscribe((dt) => { updateCount++; }, period);
            LogAssert.Expect(LogType.Error, new Regex(".*Removed anonymous.*"));
            yield return new WaitForSeconds(period + 0.1f);
            Assert.AreEqual(0, updateCount, "Lambdas should not be permitted, since they can't be Unsubscribed.");

            Locator.Get.UpdateSlow.Subscribe(ThisIsALocalFunction, period);
            LogAssert.Expect(LogType.Error, new Regex(".*Removed local function.*"));
            yield return new WaitForSeconds(period + 0.1f);
            Assert.AreEqual(0, updateCount, "Local functions should not be permitted, since they can't be Unsubscribed.");

            void ThisIsALocalFunction(float dt) { }
        }

        [UnityTest]
        public IEnumerator SubscribeNoDuplicates()
        {
            dummyOnUpdateCalls = 0;
            Locator.Get.UpdateSlow.Subscribe(DummyOnUpdate, 1);
            Locator.Get.UpdateSlow.Subscribe(DummyOnUpdate, 0.1f);

            yield return new WaitForSeconds(0.9f);
            Assert.AreEqual(0, dummyOnUpdateCalls, "The second Subscribe call should not have gone through.");

            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(1, dummyOnUpdateCalls, "The first Subscribe call should have gone through.");

            Locator.Get.UpdateSlow.Unsubscribe(DummyOnUpdate);
            yield return new WaitForSeconds(1);
            Assert.AreEqual(1, dummyOnUpdateCalls, "Unsubscribe should work as expected.");
        }
        private int dummyOnUpdateCalls = 0;
        private void DummyOnUpdate(float dt) { dummyOnUpdateCalls++; }

        [UnityTest]
        public IEnumerator WhatIfASubscriberIsVerySlow()
        {
            int updateCount = 0;
            string inefficientString = "";
            float period = 1.5f;
            Subscriber sub = new Subscriber(() => 
            {   for (int n = 0; n < 12345; n++)
                    inefficientString += n.ToString();
                updateCount++;
            }, period);
            m_activeSubscribers.Add(sub);

            LogAssert.Expect(LogType.Error, new Regex(".*took too long.*"));
            yield return new WaitForSeconds(period + 0.1f);
            Assert.AreEqual(1, updateCount, "Executed the slow update.");

            yield return new WaitForSeconds(period);
            Assert.AreEqual(1, updateCount, "Should have removed the offending subscriber.");
        }
    }
}