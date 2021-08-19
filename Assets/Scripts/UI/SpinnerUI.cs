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
            if (observed.State == LobbyQueryState.Fetching)
            {
                Show();
                spinnerImage.Show();
                noServerText.Hide();
                errorTextVisibility.Hide();
            }
            else if (observed.State == LobbyQueryState.Error)
            {
                spinnerImage.Hide();
                errorTextVisibility.Show();
                errorText.SetText("Error. See Unity Console log for details.");
            }
            else if (observed.State == LobbyQueryState.Fetched)
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
