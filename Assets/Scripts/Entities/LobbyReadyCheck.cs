using System;
using System.Collections.Generic;
using System.Linq;
using Utilities;

namespace LobbyRooms
{
    /// <summary>
    /// On the host, this will watch for all players to ready, and once they have, it will prepare for a synchronized countdown.
    /// </summary>
    public class LobbyReadyCheck : IDisposable
    {
        Action<bool> m_OnReadyCheckComplete;

        float m_ReadyTime = 5;

        public LobbyReadyCheck(Action<bool> onReadyCheckComplete = null, float readyTime = 5)
        {
            m_OnReadyCheckComplete = onReadyCheckComplete;
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
            var room = RoomsQuery.Instance.CurrentRoom;
            if (room == null || room.Players.Count == 0)
                return;
            

            int readyCount = room.Players.Count((p) =>
            {
                if (p.Data?.ContainsKey("UserStatus") != true) // Needs to be "!= true" to handle null properly.
                    return false;
                UserStatus status;
                if (Enum.TryParse(p.Data["UserStatus"].Value, out status))
                    return status == UserStatus.Ready;
                return false;
            });

            if (readyCount == room.Players.Count)
            {
                Dictionary<string, string> data = new Dictionary<string, string>();
                DateTime targetTime = DateTime.Now.AddSeconds(m_ReadyTime);
                data.Add("AllPlayersReady", targetTime.Ticks.ToString());
                RoomsQuery.Instance.UpdateRoomDataAsync(data, null);
                EndCheckingForReady(); // TODO: We'll need to restart checking once we end the relay sequence and return to the room.
            }
        }

        public void Dispose()
        {
            EndCheckingForReady();
        }
    }
}
