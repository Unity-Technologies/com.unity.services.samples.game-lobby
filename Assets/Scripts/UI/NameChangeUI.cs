namespace LobbyRelaySample.UI
{
    /// <summary>
    /// When the player changes their name with the UI, this triggers the actual rename.
    /// </summary>
    public class NameChangeUI : UIPanelBase
    {
        public void OnEndNameEdit(string name)
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.RenameRequest, name);
        }
    }
}
