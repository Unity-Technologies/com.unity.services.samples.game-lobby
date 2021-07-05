using LobbyRelaySample;
using NUnit.Framework;
using System;
using System.Text.RegularExpressions;
using UnityEngine.TestTools;

public class MessengerTests
{
    private class Subscriber : IReceiveMessages
    {
        private Action m_thingToDo;

        public Subscriber(Action thingToDo)
        {
            m_thingToDo = thingToDo;
        }

        public void OnReceiveMessage(MessageType type, object msg)
        {
            m_thingToDo?.Invoke();
        }
    }

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
        messenger.OnReceiveMessage(MessageType.None, "");

        Assert.AreEqual(1, msgCount, "Should have acted on the message.");
    }
}
