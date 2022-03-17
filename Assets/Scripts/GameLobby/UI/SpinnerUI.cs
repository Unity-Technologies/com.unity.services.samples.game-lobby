using System.Text;
using TMPro;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Controls a simple throbber that is displayed when the lobby list is being refreshed.
    /// </summary>
    public class SpinnerUI : ObserverPanel<LobbyServiceData>
    {
        [SerializeField] private TMP_Text m_errorText;
        [SerializeField] private UIPanelBase m_spinnerImage;
        [SerializeField] private UIPanelBase m_noServerText;
        [SerializeField] private UIPanelBase m_errorTextVisibility;
        [Tooltip("This prevents selecting a lobby or Joining while the spinner is visible.")]
        [SerializeField] private UIPanelBase m_raycastBlocker;

        public override void ObservedUpdated(LobbyServiceData observed)
        {
            if (observed.State == LobbyQueryState.Fetching)
            {
                Show();
                m_spinnerImage.Show();
                m_raycastBlocker.Show();
                m_noServerText.Hide();
                m_errorTextVisibility.Hide();
            }
            else if (observed.State == LobbyQueryState.Error)
            {
                m_spinnerImage.Hide();
                m_raycastBlocker.Hide();
                m_errorTextVisibility.Show();
                m_errorText.SetText("Error. See Unity Console log for details.");
            }
            else if (observed.State == LobbyQueryState.Fetched)
            {
                if (observed.CurrentLobbies.Count < 1)
                {
                    m_noServerText.Show();
                }
                else
                {
                    m_noServerText.Hide();
                }

                m_spinnerImage.Hide();
                m_raycastBlocker.Hide();
            }
        }
    }
}
