using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Watches for changes in the game state to/from the main menu.
    /// </summary>
    [RequireComponent(typeof(LocalGameStateObserver))]
    public class MainMenuUI : ObserverPanel<LocalGameState>
    {
        public override void ObservedUpdated(LocalGameState observed)
        {
            if (observed.State == GameState.Menu)
                Show();
            else
            {
                Hide();
            }
        }
    }
}
