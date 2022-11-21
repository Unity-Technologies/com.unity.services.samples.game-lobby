using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Button callbacks for the "Ready"/"Not Ready" buttons used to indicate the local player is ready/not ready.
    /// </summary>
    public class ReadyCheckUI : UIPanelBase
    {
        public void OnReadyButton()
        {
            ChangeState(PlayerStatus.Ready);
        }
        public void OnCancelButton()
        {
            ChangeState(PlayerStatus.Lobby);
        }
        void ChangeState(PlayerStatus status)
        {
            Manager.SetLocalUserStatus(status);
        }
    }
}
