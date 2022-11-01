using System;
using System.Text;
using TMPro;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Controls a simple throbber that is displayed when the lobby list is being refreshed.
    /// </summary>
    public class SpinnerUI : UIPanelBase
    {
        [SerializeField] TMP_Text m_errorText;
        [SerializeField] UIPanelBase m_spinnerImage;
        [SerializeField] UIPanelBase m_noServerText;
        [SerializeField] UIPanelBase m_errorTextVisibility;
        [Tooltip("This prevents selecting a lobby or Joining while the spinner is visible.")]
        [SerializeField] UIPanelBase m_raycastBlocker;


        public override void Start()
        {
            base.Start();
            Manager.LobbyList.QueryState.onChanged += QueryStateChanged;
        }

        void OnDestroy()
        {
            if (Manager == null)
                return;
            Manager.LobbyList.QueryState.onChanged -= QueryStateChanged;

        }

        void QueryStateChanged(LobbyQueryState state)
        {
            if (state == LobbyQueryState.Fetching)
            {
                Show();
                m_spinnerImage.Show();
                m_raycastBlocker.Show();
                m_noServerText.Hide();
                m_errorTextVisibility.Hide();
            }
            else if (state == LobbyQueryState.Error)
            {
                m_spinnerImage.Hide();
                m_raycastBlocker.Hide();
                m_errorTextVisibility.Show();
                m_errorText.SetText("Error. See Unity Console log for details.");
            }
            else if (state == LobbyQueryState.Fetched)
            {
                if (Manager.LobbyList.CurrentLobbies.Count < 1)
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
