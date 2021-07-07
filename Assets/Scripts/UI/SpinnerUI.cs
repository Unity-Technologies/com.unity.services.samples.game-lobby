using System.Text;
using TMPro;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Controls a simple throbber that is displayed when the lobby list is being refreshed.
    /// </summary>
    public class SpinnerUI : ObserverPanel<LobbyServiceData>
    {
        public TMP_Text errorText;
        public UIPanelBase spinnerImage;
        public UIPanelBase noServerText;
        public UIPanelBase errorTextVisibility;

        public override void ObservedUpdated(LobbyServiceData observed)
        {
            if (observed.State == LobbyServiceState.Fetching)
            {
                Show();
                spinnerImage.Show();
                noServerText.Hide();
                errorTextVisibility.Hide();
            }
            else if (observed.State == LobbyServiceState.Error)
            {
                spinnerImage.Hide();
                errorTextVisibility.Show();
                var errorString = new StringBuilder();
                errorString.Append("Error");
                var codeString = ": " + observed.lastErrorCode;
                if (observed.lastErrorCode < 1)
                    codeString = ".";

                errorString.Append(codeString);
                errorText.SetText(errorString.ToString());
            }
            else if (observed.State == LobbyServiceState.Fetched)
            {
                if (observed.CurrentLobbies.Count < 1)
                {
                    noServerText.Show();
                }
                else
                {
                    noServerText.Hide();
                }

                spinnerImage.Hide();
            }
        }
    }
}
