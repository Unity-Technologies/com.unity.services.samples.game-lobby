using TMPro;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Read Only input field (for copy/paste reasons) Watches for the changes in the lobby's Room Code
    /// </summary>
    public class RoomCodeUI : ObserverPanel<LocalLobby>
    {
        public TMP_InputField roomCodeText;

        public override void ObservedUpdated(LocalLobby observed)
        {
            if (!string.IsNullOrEmpty(observed.LobbyCode))
            {
                roomCodeText.text = observed.LobbyCode;
                Show();
            }
            else
            {
                Hide();
            }
           
        }
    }
}
