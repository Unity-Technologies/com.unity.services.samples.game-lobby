using LobbyRelaySample.UI;

namespace LobbyRelaySample
{
    /// <summary>
    /// Main menu start button.
    /// </summary>
    public class StartLobbyButtonUI : UIPanelBase
    {
        public void ToJoinMenu()
        {
            Manager.UIChangeMenuState(GameState.JoinMenu);
        }
    }
}
