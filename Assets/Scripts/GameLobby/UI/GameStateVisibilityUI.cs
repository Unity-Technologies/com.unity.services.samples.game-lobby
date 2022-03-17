using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Show or hide a UI element based on the current GameState (e.g. in a lobby).
    /// </summary>
    [RequireComponent(typeof(LocalMenuStateObserver))]
    public class GameStateVisibilityUI : ObserverPanel<LocalMenuState>
    {
        [SerializeField]
        GameState ShowThisWhen;

        public override void ObservedUpdated(LocalMenuState observed)
        {
            if (!ShowThisWhen.HasFlag(observed.State))
                Hide();
            else
            {
                Show();
            }
        }
    }
}
