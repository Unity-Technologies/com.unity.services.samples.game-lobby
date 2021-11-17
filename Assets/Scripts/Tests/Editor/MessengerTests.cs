using LobbyRelaySample;
using NUnit.Framework;
using System;
using System.Text.RegularExpressions;
using UnityEngine.TestTools;

namespace Test
{
    public class MessengerTests
    {
        #region Test classes
        /// <summary>Trivial message recipient that will run some action on any message.</summary>
        private class Subscriber : IReceiveMessages
        {
            private Action m_thingToDo;
            public Subscriber(Action thingToDo) { m_thingToDo = thingToDo; }
            public void OnReceiveMessage(MessageType type, object msg) { m_thingToDo?.Invoke(); }
        }
        /// <summary>Trivial message recipient that will run some action on any message, with args.</summary>
        private class SubscriberArgs : IReceiveMessages
        {
            private Action<MessageType, object> m_thingToDo;
            public SubscriberArgs(Action<MessageType, object> thingToDo) { m_thingToDo = thingToDo; }
            public void OnReceiveMessage(MessageType type, object msg) { m_thingToDo?.Invoke(type, msg); }
        }
        /// <summary>Trivial message recipient that will run some action on any message and then unsubscribe.</summary>
        private class SubscriberUnsub : IReceiveMessages
        {
            private Action m_thingToDo;
            private Messenger m_messenger;
            public SubscriberUnsub(Action thingToDo, Messenger messenger) { m_thingToDo = thingToDo; m_messenger = messenger; }
            public void OnReceiveMessage(MessageType type, object msg) { m_thingToDo?.Invoke(); m_messenger.Unsubscribe(this); }
        }

        #endregion

        [SetUp]
        public void Setup()
        {
            LogHandler.Get().mode = LogMode.Verbose; // Some tests rely on log messages appearing. This is reset when entering Play mode, so it's safe to set it arbitrarily here.
        }

        [Test]
        public void BasicBehavior()
        {
            Messenger messenger = new Messenger();
            int msgCount = 0;
            SubscriberArgs sub = new SubscriberArgs((type, msg) => { 
                msgCount++; // These are just for simple detection of the intended behavior.
                if (type == MessageType.RenameRequest) msgCount += 9;
                if (msg is string) msgCount += int.Parse(msg as string);
            });

            messenger.OnReceiveMessage(MessageType.None, null);
            Assert.AreEqual(0, msgCount, "Should not act on message until Subscribed.");

            messenger.Subscribe(sub);
            messenger.OnReceiveMessage(MessageType.None, null);
            Assert.AreEqual(1, msgCount, "Should act on message once Subscribed");
            messenger.OnReceiveMessage(MessageType.None, null);
            Assert.AreEqual(2, msgCount, "Should act on message once Subscribed, again");
            messenger.OnReceiveMessage(MessageType.RenameRequest, null);
            Assert.AreEqual(12, msgCount, "Should provide the correct message type.");
            messenger.OnReceiveMessage(MessageType.None, "99");
            Assert.AreEqual(112, msgCount, "Should provide the msg object.");

            messenger.Subscribe(sub);
            messenger.OnReceiveMessage(MessageType.None, null);
            Assert.AreEqual(113, msgCount, "Should not duplicate subscription.");

            messenger.Unsubscribe(sub);
            messenger.OnReceiveMessage(MessageType.None, "36");
            Assert.AreEqual(113, msgCount, "Should not message subscriber once Unsubscribed.");

            messenger.Subscribe(sub);
            messenger.OnReceiveMessage(MessageType.None, "36");
            Assert.AreEqual(150, msgCount, "Should receive messages on resubscription.");
            messenger.Unsubscribe(sub);
        }

        [Test]
        public void BasicBehavior_Multiple()
        {
            Messenger messenger = new Messenger();
            int msgCount = 0;
            SubscriberArgs sub1 = new SubscriberArgs(UpdateMsgCount);
            SubscriberArgs sub2 = new SubscriberArgs(UpdateMsgCount);

            messenger.OnReceiveMessage(MessageType.None, null);
            Assert.AreEqual(0, msgCount, "Base case");

            messenger.Subscribe(sub1);
            messenger.OnReceiveMessage(MessageType.None, null);
            Assert.AreEqual(1, msgCount, "First subscriber");

            messenger.Subscribe(sub2);
            messenger.OnReceiveMessage(MessageType.None, null);
            Assert.AreEqual(3, msgCount, "Both subscribers should get the message.");

            messenger.Unsubscribe(sub1);
            messenger.OnReceiveMessage(MessageType.None, null);
            Assert.AreEqual(4, msgCount, "Second subscriber should not be affected by first subscriber's Unsubscribe.");

            messenger.Unsubscribe(sub2);
            messenger.OnReceiveMessage(MessageType.None, null);
            Assert.AreEqual(4, msgCount, "No Unsubscribed subscriber should get messages.");

            void UpdateMsgCount(MessageType type, object msg)
            {
                msgCount++;
                if (type == MessageType.RenameRequest) msgCount += 9;
                if (msg is string) msgCount += int.Parse(msg as string);
            }
        }

        [Test]
        public void SubAndUnsubRepeatedly()
        {
            Messenger messenger = new Messenger();
            int msgCount = 0;
            Subscriber sub1 = new Subscriber(() => { msgCount++; });
            Subscriber sub2 = new Subscriber(() => { msgCount += 10; });
            Subscriber sub3 = new Subscriber(() => { msgCount += 100; });

            messenger.Subscribe(sub1);
            messenger.OnReceiveMessage(MessageType.None, null);
            Assert.AreEqual(1, msgCount, "Initial state");

            messenger.Unsubscribe(sub1);
            messenger.Unsubscribe(sub2);
            messenger.Subscribe(sub2);
            messenger.Subscribe(sub1);
            messenger.Subscribe(sub3);
            messenger.Subscribe(sub3);

            messenger.OnReceiveMessage(MessageType.None, null);
            Assert.AreEqual(112, msgCount, "Should have all three subscribers registered once.");
        }

        [Test]
        public void SubscribeWithinOnReceiveMessage()
        {
            Messenger messenger = new Messenger();
            int msgCount = 0;
            Subscriber sub1 = new Subscriber(UpdateMsgCountWithSubscribe);

            messenger.Subscribe(sub1);
            messenger.OnReceiveMessage(MessageType.None, null);
            Assert.AreEqual(1, msgCount, "First subscriber adds another when receiving a message, but the new subscriber shouldn't immediately receive the same message.");

            messenger.OnReceiveMessage(MessageType.None, null);
            Assert.AreEqual(3, msgCount, "First subscriber adds a third on another message; second subscriber gets this message.");

            messenger.Unsubscribe(sub1);
            messenger.OnReceiveMessage(MessageType.None, null);
            Assert.AreEqual(5, msgCount, "Original subscriber is gone; two added subscribers persist.");

            messenger.OnReceiveMessage(MessageType.None, null);
            Assert.AreEqual(7, msgCount, "Confirming there are just the two subscribers.");

            void UpdateMsgCountWithSubscribe()
            {
                msgCount++;
                messenger.Subscribe(new Subscriber(UpdateMsgCount));
            }

            void UpdateMsgCount()
            {
                msgCount++;
            }
        }

        [Test]
        public void UnsubscribeWithinOnReceiveMessage()
        {
            Messenger messenger = new Messenger();
            int msgCount = 0;
            SubscriberUnsub sub1 = new SubscriberUnsub(UpdateMsgCount, messenger);
            Subscriber sub2 = new Subscriber(UpdateMsgCount);

            messenger.Subscribe(sub1);
            messenger.OnReceiveMessage(MessageType.None, null);
            Assert.AreEqual(1, msgCount, "Message received by subscriber.");
            messenger.OnReceiveMessage(MessageType.None, null);
            Assert.AreEqual(1, msgCount, "Unsubscribed as part of OnReceiveMessage.");

            messenger.Subscribe(sub1);
            messenger.Subscribe(sub2);
            messenger.OnReceiveMessage(MessageType.None, null);
            Assert.AreEqual(3, msgCount, "Both subscribed subs get the message.");
            messenger.OnReceiveMessage(MessageType.None, null);
            Assert.AreEqual(4, msgCount, "One sub is still subscribed.");

            messenger.Subscribe(sub1);
            messenger.OnReceiveMessage(MessageType.None, null);
            Assert.AreEqual(6, msgCount, "Both subscribed subs get the message (reversed order).");
            messenger.OnReceiveMessage(MessageType.None, null);
            Assert.AreEqual(7, msgCount, "One sub is again still subscribed.");


            void UpdateMsgCount()
            {
                msgCount++;
            }
        }

        /// <summary>
        /// If a message recipient takes a long time to process a message, we want to be made aware.
        /// </summary>
        [Test]
        public void WhatIfAMessageIsVerySlow()
        {
            Messenger messenger = new Messenger();
            int msgCount = 0;
            string inefficientString = "";
            Subscriber sub = new Subscriber(() =>
            {   for (int n = 0; n < 12345; n++)
                        inefficientString += n.ToString();
                    msgCount++;
            });
            messenger.Subscribe(sub);

            LogAssert.Expect(UnityEngine.LogType.Warning, new Regex(".*took too long.*"));
            messenger.OnReceiveMessage(MessageType.None, null);

            Assert.AreEqual(1, msgCount, "Should have acted on the message.");
        }
    }
}