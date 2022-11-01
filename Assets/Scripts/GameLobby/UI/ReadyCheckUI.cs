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
            ChangeState(UserStatus.Ready);
        }
        public void OnCancelButton()
        {
            ChangeState(UserStatus.Lobby);
        }
        void ChangeState(UserStatus status)
        {
            Manager.SetLocalUserStatus(status);
        }
    }
}
