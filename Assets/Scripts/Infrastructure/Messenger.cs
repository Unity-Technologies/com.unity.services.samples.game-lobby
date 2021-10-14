using System.Collections.Generic;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace LobbyRelaySample
{
    /// <summary>
    /// Core mechanism for routing messages to arbitrary listeners.
    /// This allows components with unrelated responsibilities to interact without becoming coupled, since message senders don't
    /// need to know what (if anything) is receiving their messages.
    /// </summary>
    public class Messenger : IMessenger
    {
        private List<IReceiveMessages> m_receivers = new List<IReceiveMessages>();
        private const float k_durationToleranceMs = 10;

        /// <summary>
        /// Assume that you won't receive messages in a specific order.
        /// </summary>
        public virtual void Subscribe(IReceiveMessages receiver)
        {
            if (!m_receivers.Contains(receiver))
                m_receivers.Add(receiver);
        }

        public virtual void Unsubscribe(IReceiveMessages receiver)
        {
            m_receivers.Remove(receiver);
        }

        /// <summary>
        /// Send a message to any subscribers, who will decide how to handle the message.
        /// </summary>
        /// <param name="msg">If there's some data relevant to the recipient, include it here.</param>
        public virtual void OnReceiveMessage(MessageType type, object msg)
        {
            Stopwatch stopwatch = new Stopwatch();
            for (int r = 0; r < m_receivers.Count; r++)
            {
                stopwatch.Restart();
                m_receivers[r].OnReceiveMessage(type, msg);
                stopwatch.Stop();
                if (stopwatch.ElapsedMilliseconds > k_durationToleranceMs)
                    Debug.LogWarning($"Message recipient \"{m_receivers[r]}\" took too long to process message \"{msg}\" of type {type}");
            }
        }

        public void OnReProvided(IMessenger previousProvider)
        {
            if (previousProvider is Messenger)
                m_receivers.AddRange((previousProvider as Messenger).m_receivers);
        }
    }

    /// <summary>
    /// Ensure that message contents are obvious but not dependent on spelling strings correctly.
    /// </summary>
    public enum MessageType
    {
        // These are assigned arbitrary explicit values so that if a MessageType is serialized and more enum values are later inserted/removed, the serialized values need not be reassigned.
        // (If you want to remove a message, make sure it isn't serialized somewhere first.)
        None = 0,
        RenameRequest = 1,
        JoinLobbyRequest = 2,
        CreateLobbyRequest = 3,
        QueryLobbies = 4,
        QuickJoin = 5,

        ChangeGameState = 100,
        ConfirmInGameState = 101,
        LobbyUserStatus = 102,
        UserSetEmote = 103,
        ClientUserApproved = 104,
        ClientUserSeekingDisapproval = 105,
        EndGame = 106,

        StartCountdown = 200,
        CancelCountdown = 201,
        CompleteCountdown = 202,

        DisplayErrorPopup = 300,
    }

    /// <summary>
    /// Something that wants to subscribe to messages from arbitrary, unknown senders.
    /// </summary>
    public interface IReceiveMessages
    {
        void OnReceiveMessage(MessageType type, object msg);
    }

    /// <summary>
    /// Something to which IReceiveMessages can send/subscribe for arbitrary messages.
    /// </summary>
    public interface IMessenger : IReceiveMessages, IProvidable<IMessenger>
    {
        void Subscribe(IReceiveMessages receiver);
        void Unsubscribe(IReceiveMessages receiver);
    }
}
