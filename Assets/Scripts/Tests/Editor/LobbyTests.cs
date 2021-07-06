using LobbyRelaySample;
using NUnit.Framework;

namespace Test
{
    public class LobbyTests
    {
        LocalLobby m_LocalLobby;

        const int k_TestUserCount = 3;

        [SetUp]
        public void Setup()
        {
            m_LocalLobby = new LocalLobby();

            for (int i = 0; i < k_TestUserCount; i++)
            {
                m_LocalLobby.AddPlayer(new LobbyUser
                {
                    ID = i.ToString()
                });
            }
        }

        [Test]
        public void LobbyPlayerStateTest()
        {
            Assert.False(m_LocalLobby.PlayersOfState(UserStatus.Ready));

            m_LocalLobby.LobbyUsers["0"].UserStatus = UserStatus.Ready;
            Assert.False(m_LocalLobby.PlayersOfState(UserStatus.Ready));
            Assert.True(m_LocalLobby.PlayersOfState(UserStatus.Ready, 1));

            m_LocalLobby.LobbyUsers["1"].UserStatus = UserStatus.Ready;
            Assert.False(m_LocalLobby.PlayersOfState(UserStatus.Ready));
            Assert.True(m_LocalLobby.PlayersOfState(UserStatus.Ready, 2));

            m_LocalLobby.LobbyUsers["2"].UserStatus = UserStatus.Ready;

            Assert.True(m_LocalLobby.PlayersOfState(UserStatus.Ready));
        }
    }
}
