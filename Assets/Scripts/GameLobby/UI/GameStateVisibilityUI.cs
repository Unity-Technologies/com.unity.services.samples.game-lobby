using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Show or hide a UI element based on the current GameState (e.g. in a lobby).
    /// </summary>
    public class GameStateVisibilityUI : UIPanelBase
    {
        [SerializeField]
        GameState ShowThisWhen;

        void GameStateChanged(GameState state)
        {
            if (!ShowThisWhen.HasFlag(state))
                Hide();
            else
                Show();
        }

        public override void Start()
        {
            base.Start();
            Manager.onGameStateChanged += GameStateChanged;
        }

        void OnDestroy()
        {
            if (Manager == null)
                return;
            Manager.onGameStateChanged -= GameStateChanged;
        }
    }
}
