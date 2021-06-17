using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using Assert = UnityEngine.Assertions.Assert;

namespace LobbyRooms.Tests
{
    public class ObserverTests
    {
        [UnityTest]
        public IEnumerator ObserverChangeWhenObservedChanged() // Test if Observer changes when StringField gets set
        {
            var observed = new TestObserved();
            var observer = new GameObject("PlayerObserver").AddComponent<TestObservereBehaviour>();

            observer.BeginObserving(observed);
            Assert.AreNotEqual("NewName", observed.StringField);
            Assert.AreNotEqual("NewName", observer.displayStringField);

            observed.StringField = "NewName";
            yield return null;

            Assert.AreEqual(observed.StringField, observer.displayStringField);
        }

        [UnityTest]
        public IEnumerator ObserverRegistersInitialChanges() // Test if Observer changes on Initialization
        {
            var observed = new TestObserved();
            observed.StringField = "NewName"; // Set the field before we begin observing

            var observer = new GameObject("PlayerObserver").AddComponent<TestObservereBehaviour>();
            Assert.AreNotEqual(observed.StringField, observer.displayStringField);

            observer.BeginObserving(observed);
            yield return null;

            Assert.AreEqual(observed.StringField, observer.displayStringField);
        }

        class TestObserved : Observed<TestObserved>
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

        //Mock UI Observer
        class TestObservereBehaviour : ObserverBehaviour<TestObserved>
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
