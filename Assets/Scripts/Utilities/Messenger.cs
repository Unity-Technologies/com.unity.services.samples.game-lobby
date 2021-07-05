using System.Collections.Generic;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace LobbyRelaySample
{
    /// <summary>
    /// Ensure that message contents are obvious but not dependent on spelling strings correctly.
    /// </summary>
    public enum MessageType
    {
        // These are assigned arbitrary explicit values so that if a MessageType is serialized and more enum values are later inserted/removed, the serialized values need not be reassigned.
        // (If you want to remove a message, make sure it isn't serialized somewhere first.)
        None = 0,
        RenameRequest = 1,
        JoinRoomRequest = 2,
        CreateRoomRequest = 3,
        QueryRooms = 4,
        PlayerJoinedRoom = 5,
        PlayerLeftRoom = 6,
        ChangeGameState = 7,
        ChangeLobbyUserState = 8,
        HostInitReadyCheck = 9,
        LocalUserReadyCheckResponse = 10,
        UserSetEmote = 11,
        ToLobby = 12,
        Client_EndReadyCountdownAt = 13,
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

    /// <summary>
    /// Core mechanism for routing messages to arbitrary listeners.
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
}
