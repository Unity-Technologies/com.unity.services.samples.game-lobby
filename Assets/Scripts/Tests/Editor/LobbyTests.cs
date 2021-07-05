using LobbyRelaySample;
using NUnit.Framework;

namespace Test
{
    public class LobbyTests
    {
        LobbyData m_LobbyData;

        const int k_TestUserCount = 3;

        [SetUp]
        public void Setup()
        {
            m_LobbyData = new LobbyData();

            for (int i = 0; i < k_TestUserCount; i++)
            {
                m_LobbyData.AddPlayer(new LobbyUser
                {
                    ID = i.ToString()
                });
            }
        }

        [Test]
        public void LobbyPlayerStateTest()
        {
            Assert.False(m_LobbyData.PlayersOfState(UserStatus.Ready));

            m_LobbyData.LobbyUsers["0"].UserStatus = UserStatus.Ready;
            Assert.False(m_LobbyData.PlayersOfState(UserStatus.Ready));
            Assert.True(m_LobbyData.PlayersOfState(UserStatus.Ready, 1));

            m_LobbyData.LobbyUsers["1"].UserStatus = UserStatus.Ready;
            Assert.False(m_LobbyData.PlayersOfState(UserStatus.Ready));
            Assert.True(m_LobbyData.PlayersOfState(UserStatus.Ready, 2));

            m_LobbyData.LobbyUsers["2"].UserStatus = UserStatus.Ready;

            Assert.True(m_LobbyData.PlayersOfState(UserStatus.Ready));
        }
    }
}
