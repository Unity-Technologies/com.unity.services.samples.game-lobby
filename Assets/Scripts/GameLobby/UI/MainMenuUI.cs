using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Watches for changes in the game state to/from the main menu.
    /// </summary>
    [RequireComponent(typeof(LocalMenuStateObserver))]
    public class MainMenuUI : ObserverPanel<LocalMenuState>
    {
        public override void ObservedUpdated(LocalMenuState observed)
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
