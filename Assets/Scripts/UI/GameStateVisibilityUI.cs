using UnityEngine;

namespace LobbyRooms.UI
{
    [RequireComponent(typeof(LocalGameStateObserver))]
    public class GameStateVisibilityUI : ObserverPanel<LocalGameState>
    {
        [SerializeField]
        GameState ShowThisWhen;

        public override void ObservedUpdated(LocalGameState observed)
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
