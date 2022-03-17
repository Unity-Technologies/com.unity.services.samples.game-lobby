using System;
using UnityEngine;

namespace LobbyRelaySample
{
    /// <summary>
    /// Runs the countdown to the in-game state. While the start of the countdown is synced via Relay, the countdown itself is handled locally,
    /// since precise timing isn't necessary.
    /// </summary>
    [RequireComponent(typeof(UI.CountdownUI))]
    public class Countdown : MonoBehaviour, IReceiveMessages
    {
        public class Data : Observed<Countdown.Data>
        {
            private float m_timeLeft;
            public float TimeLeft 
            {
                get => m_timeLeft;
                set
                {   m_timeLeft = value;
                    OnChanged(this);
                }
            }
            public override void CopyObserved(Data oldObserved) { /*No-op, since this is unnecessary.*/ }
        }

        private Data m_data = new Data();
        private UI.CountdownUI m_ui;
        private const int k_countdownTime = 4;

        public void OnEnable()
        {
            if (m_ui == null)
                m_ui = GetComponent<UI.CountdownUI>();
            m_data.TimeLeft = -1;
            Locator.Get.Messenger.Subscribe(this);
            m_ui.BeginObserving(m_data);
        }
        public void OnDisable()
        {
            Locator.Get.Messenger.Unsubscribe(this);
            m_ui.EndObserving();
        }

        public void OnReceiveMessage(MessageType type, object msg)
        {
            if (type == MessageType.StartCountdown)
            {
                m_data.TimeLeft = k_countdownTime;
            }
            else if (type == MessageType.CancelCountdown)
            {
                m_data.TimeLeft = -1;
            }
        }

        public void Update()
        {
            if (m_data.TimeLeft < 0)
                return;
            m_data.TimeLeft -= Time.deltaTime;
            if (m_data.TimeLeft < 0)
                Locator.Get.Messenger.OnReceiveMessage(MessageType.CompleteCountdown, null);
        }
    }
}
