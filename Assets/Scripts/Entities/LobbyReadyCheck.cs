using System;
using UnityEngine;
using Utilities;

namespace LobbyRooms
{
    public class LobbyReadyCheck : IDisposable
    {
        public bool ReadyCheckFinished { get; private set; }
        public bool ReadyCheckSuccess { get; private set; }
        Action<bool> m_OnReadyCheckComplete;

        const float k_Checkinterval = 0.5f;
        LobbyData m_LobbyData;
        float m_ReadyTime = 10;

        public LobbyReadyCheck(LobbyData lobbyData, Action<bool> onReadyCheckComplete, float readyTime = 10)
        {
            m_OnReadyCheckComplete = onReadyCheckComplete;

            m_LobbyData = lobbyData;
            m_LobbyData.SetAllPlayersToState(UserStatus.ReadyCheck);
            m_ReadyTime = readyTime;
            Locator.Get.UpdateSlow.Subscribe(CheckIfReady);
        }

        float m_CheckCount;
        float m_TimeElapsed = 0;

        /// <summary>
        /// Checks the lobby to see if we have all Readied up, or any one has cancelled.
        /// NOTE: The countdown will be happening at different times with this setup, possibility of a desynched game start.
        /// Or a player cancellign the last milliseconds resulting in players starting without him
        /// </summary>
        void CheckIfReady(float dt)
        {
            m_TimeElapsed += dt;

            if (m_CheckCount < k_Checkinterval)
            {
                m_CheckCount += dt;
                return;
            }

            m_CheckCount = 0;
            if (m_TimeElapsed + 1 < m_ReadyTime && m_LobbyData.PlayersOfState(UserStatus.Cancelled, 1)) //Dont allow cancels near the end of the ready check
            {
                FinishedReadyCheck(false);
            }
            else if (m_LobbyData.PlayersOfState(UserStatus.Ready))
            {
                FinishedReadyCheck(true);
            }
            else if (m_TimeElapsed > m_ReadyTime)
            {
                FinishedReadyCheck(false);
            }
        }

        void FinishedReadyCheck(bool success)
        {
            ReadyCheckSuccess = success;
            ReadyCheckFinished = true;
            m_OnReadyCheckComplete?.Invoke(success);
        }

        public void Dispose()
        {
            Locator.Get.UpdateSlow.Unsubscribe(CheckIfReady);
        }
    }
}
