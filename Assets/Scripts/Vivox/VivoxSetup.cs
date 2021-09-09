using System;
using System.Collections.Generic;
using Unity.Services.Vivox;
using VivoxUnity;

namespace LobbyRelaySample.vivox
{
    /// <summary>
    /// Handles setting up a voice channel once inside a lobby.
    /// </summary>
    public class VivoxSetup
    {
        private bool m_hasInitialized = false;
        private bool m_isMidInitialize = false;
        private ILoginSession m_loginSession = null;
        private IChannelSession m_channelSession = null;
        private List<VivoxUserHandler> m_userHandlers;

        /// <summary>
        /// Initialize the Vivox service, before actually joining any audio channels.
        /// </summary>
        public void Initialize(List<VivoxUserHandler> userHandlers)
        {
            if (m_isMidInitialize)
                return;
            m_isMidInitialize = true;

            m_userHandlers = userHandlers;
            VivoxService.Instance.Initialize();
            Account account = new Account(Locator.Get.Identity.GetSubIdentity(Auth.IIdentityType.Auth).GetContent("id"));
            m_loginSession = VivoxService.Instance.Client.GetLoginSession(account);
            string token = m_loginSession.GetLoginToken();

            m_loginSession.BeginLogin(m_loginSession.GetLoginToken(), SubscriptionMode.Accept, null, null, null, result =>
            {
                try
                {
                    m_loginSession.EndLogin(result);
                    m_hasInitialized = true;
                }
                finally
                {
                    m_isMidInitialize = false;
                }
            });
        }

        /// <summary>
        /// Once in a lobby, start joining a voice channel for that lobby. Be sure to complete Initialize first.
        /// </summary>
        /// <param name="onComplete">Called whether the channel is successfully joined or not.</param>
        public void JoinLobbyChannel(string lobbyId, Action<bool> onComplete)
        {
            if (!m_hasInitialized || m_loginSession.State != LoginState.LoggedIn)
            {
                UnityEngine.Debug.LogWarning("Can't join a Vivox audio channel, as Vivox login hasn't completed yet.");
                onComplete?.Invoke(false);
                return;
            }

            ChannelType channelType = ChannelType.NonPositional;
            Channel channel = new Channel(lobbyId + "_voice", channelType, null);
            m_channelSession = m_loginSession.GetChannelSession(channel);
            string token = m_channelSession.GetConnectToken();

            m_channelSession.BeginConnect(true, false, true, m_channelSession.GetConnectToken(), result =>
            {
                m_channelSession.EndConnect(result); // TODO: Error handling?
                onComplete?.Invoke(true);
                foreach (VivoxUserHandler userHandler in m_userHandlers)
                    userHandler.OnChannelJoined(m_channelSession);
            });
        }

        // TODO: Reset slider and mute UI on lobby enter

        /// <summary>
        /// To be called when leaving a lobby.
        /// </summary>
        public void LeaveLobbyChannel()
        {
            ChannelId id = m_channelSession.Channel;
            m_channelSession?.Disconnect(
                (result) => { m_loginSession.DeleteChannelSession(id); }); // TODO: What about if this is called while also trying to connect?
            foreach (VivoxUserHandler userHandler in m_userHandlers)
                userHandler.OnChannelLeft();
        }

        /// <summary>
        /// To be called on quit, this will disconnect the player from Vivox entirely instead of just leaving any open lobby channels.
        /// </summary>
        public void Uninitialize()
        {
            // TODO: Also call LeaveLobbyChannel?

            if (!m_hasInitialized)
                return;
            m_loginSession.Logout();
        }
    }
}
