using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using Assert = UnityEngine.Assertions.Assert;

namespace LobbyRelaySample.Tests
{
    public class ObserverTests
    {
        /// <summary>
        /// When an observed value changes, the Observer should automatically update.
        /// </summary>
        [UnityTest]
        public IEnumerator ObserverChangeWhenObservedChanged()
        {
            var observed = new TestObserved();
            var observer = new GameObject("PlayerObserver").AddComponent<TestObserverBehaviour>();

            observer.BeginObserving(observed);
            Assert.AreNotEqual("NewName", observed.StringField);
            Assert.AreNotEqual("NewName", observer.displayStringField);

            observed.StringField = "NewName";
            yield return null;

            Assert.AreEqual(observed.StringField, observer.displayStringField);
        }

        /// <summary>
        /// When an Observer is registered, it should receive the observed field's initial value.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator ObserverRegistersInitialChanges()
        {
            var observed = new TestObserved();
            observed.StringField = "NewName";

            var observer = new GameObject("PlayerObserver").AddComponent<TestObserverBehaviour>();
            Assert.AreNotEqual(observed.StringField, observer.displayStringField);

            observer.BeginObserving(observed);
            yield return null;

            Assert.AreEqual(observed.StringField, observer.displayStringField);
        }

        // We just have a couple Observers that update some arbitrary member, in this case a string.
        private class TestObserved : Observed<TestObserved>
        {
            string m_stringField;

            public string StringField
            {
                get => m_stringField;
                set
                {
                    m_stringField = value;
                    OnChanged(this);
                }
            }

            public override void CopyObserved(TestObserved oldObserved)
            {
                m_stringField = oldObserved.StringField;
                OnChanged(this);
            }
        }

        private class TestObserverBehaviour : ObserverBehaviour<TestObserved>
        {
            public string displayStringField;

            protected override void UpdateObserver(TestObserved observed)
            {
                base.UpdateObserver(observed);
                displayStringField = observed.StringField;
            }
        }
    }
}
