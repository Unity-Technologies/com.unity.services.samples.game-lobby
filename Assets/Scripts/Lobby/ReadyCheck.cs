using System;
using System.Collections.Generic;
using System.Linq;

namespace LobbyRelaySample
{
    /// <summary>
    /// On the host, this will watch for all players to ready, and once they have, it will prepare for a synchronized countdown.
    /// </summary>
    public class ReadyCheck : IDisposable
    {
        float m_ReadyTime = 5;

        public ReadyCheck(float readyTime = 5)
        {
            m_ReadyTime = readyTime;
        }

        public void BeginCheckingForReady()
        {
            Locator.Get.UpdateSlow.Subscribe(OnUpdate);
        }

        public void EndCheckingForReady()
        {
            Locator.Get.UpdateSlow.Unsubscribe(OnUpdate);
        }

        /// <summary>
        /// Checks the lobby to see if we have all Readied up. If so, send out a message with the target time at which to end a countdown.
        /// </summary>
        void OnUpdate(float dt)
        {
            var lobby = LobbyAsyncRequests.Instance.CurrentLobby;
            if (lobby == null || lobby.Players.Count == 0)
                return;

            int readyCount = lobby.Players.Count((p) =>
            {
                if (p.Data?.ContainsKey("UserStatus") != true) // Needs to be "!= true" to handle null properly.
                    return false;
                UserStatus status;
                if (Enum.TryParse(p.Data["UserStatus"].Value, out status))
                    return status == UserStatus.Ready;
                return false;
            });

            if (readyCount == lobby.Players.Count)
            {
                Dictionary<string, string> data = new Dictionary<string, string>();
                DateTime targetTime = DateTime.Now.AddSeconds(m_ReadyTime);
                data.Add("AllPlayersReady", targetTime.Ticks.ToString());
                LobbyAsyncRequests.Instance.UpdateLobbyDataAsync(data, null);
                EndCheckingForReady();
            }
        }

        public void Dispose()
        {
            EndCheckingForReady();
        }
    }
}
