using Utilities;

namespace LobbyRooms.UI
{
    /// <summary>
    /// The panel that holds the room joining and creation panels TODO if we end up not needing this, replace with UIPanelBase on the prefab
    /// </summary>
    public class JoinCreateRoomUI : ObserverPanel<LocalGameState>
    {
        public override void ObservedUpdated(LocalGameState observed)
        {
            if (observed.State == GameState.JoinMenu)
            {
                Show();
                Locator.Get.Messenger.OnReceiveMessage(MessageType.QueryRooms, null);
            }
            else
            {
                Hide();
            }
        }
    }
}
