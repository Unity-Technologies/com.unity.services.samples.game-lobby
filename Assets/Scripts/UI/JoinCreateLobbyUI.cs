namespace LobbyRelaySample.UI
{
    /// <summary>
    /// The panel that holds the lobby joining and creation panels.
    /// </summary>
    public class JoinCreateLobbyUI : ObserverPanel<LocalGameState>
    {
        public override void ObservedUpdated(LocalGameState observed)
        {
            if (observed.State == GameState.JoinMenu)
            {
                Show(false);
            }
            else
            {
                Hide();
            }
        }
        
        
        
    }
}
