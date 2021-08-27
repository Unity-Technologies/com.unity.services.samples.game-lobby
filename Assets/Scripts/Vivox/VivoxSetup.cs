using System;
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

        /// <summary>
        /// Initialize the Vivox service, before actually joining any audio channels.
        /// </summary>
        /// <param name="onComplete">Called on complete, whether successful or not. Not called if already in the middle of a previous Initialize call.</param>
        public void Initialize(Action onComplete)
        {
            if (m_isMidInitialize)
                return;
            m_isMidInitialize = true;

            VivoxService.Instance.Initialize();
            Account account = new Account(Locator.Get.Identity.GetSubIdentity(Auth.IIdentityType.Auth).GetContent("id"));
            m_loginSession = VivoxService.Instance.Client.GetLoginSession(account);

            m_loginSession.BeginLogin(m_loginSession.GetLoginToken(), SubscriptionMode.Accept, null, null, null, result =>
            {
                try
                {
                    m_loginSession.EndLogin(result);
                    m_hasInitialized = true;
                }
                catch
                {
                    throw;
                }
                finally
                {
                    m_isMidInitialize = false;
                    onComplete?.Invoke(); // TODO: Is BeginLogin guaranteed to call this callback?
                }
            });
        }

        /// <summary>
        /// Once in a lobby, start joining a voice channel for that lobby. Be sure to complete Initialize first.
        /// </summary>
        public void JoinLobbyChannel(string lobbyId)
        {
            if (!m_hasInitialized || m_loginSession.State != LoginState.LoggedIn)
            {
                UnityEngine.Debug.LogWarning("Can't join a Vivox audio channel, as Vivox login hasn't completed yet.");
                return;
            }

            ChannelType channelType = ChannelType.NonPositional;
            Channel channel = new Channel(lobbyId + "_voice", channelType, null);
            m_channelSession = m_loginSession.GetChannelSession(channel);

            m_channelSession.BeginConnect(true, false, true, m_channelSession.GetConnectToken(), result =>
            {
                m_channelSession.EndConnect(result); // TODO: Error handling?
            });
        }

        /// <summary>
        /// To be called when leaving a lobby.
        /// </summary>
        public void LeaveLobbyChannel()
        {
            ChannelId id = m_channelSession.Channel;
            m_channelSession?.Disconnect(
                (result) => { m_loginSession.DeleteChannelSession(id); }); // TODO: What about if this is called while also trying to connect?
        }

        /// <summary>
        /// To be called on quit, this will disconnect the player from Vivox entirely instead of just leaving any open lobby channels.
        /// </summary>
        public void Uninitialize()
        {
            if (!m_hasInitialized)
                return;
            m_loginSession.Logout();
        }
    }
}
