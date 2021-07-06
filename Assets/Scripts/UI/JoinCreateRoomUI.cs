namespace LobbyRelaySample.UI
{
    /// <summary>
    /// The panel that holds the room joining and creation panels.
    /// </summary>
    public class JoinCreateRoomUI : ObserverPanel<LocalGameState>
    {
        public override void ObservedUpdated(LocalGameState observed)
        {
            if (observed.State == GameState.JoinMenu)
            {
                Show();
                Locator.Get.Messenger.OnReceiveMessage(MessageType.QueryLobbies, null);
            }
            else
            {
                Hide();
            }
        }
    }
}
