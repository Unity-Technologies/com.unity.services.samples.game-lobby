using UnityEngine;
using UnityEngine.Events;

namespace LobbyRooms.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class UIPanelBase : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent<bool> m_onVisibilityChange;

        CanvasGroup m_canvasGroup;

        protected CanvasGroup MyCanvasGroup
        {
            get
            {
                if (m_canvasGroup != null) return m_canvasGroup;
                return m_canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        [SerializeField] // TODO: Why serialized? Just for testing?
        bool showing;

        // TODO: Initial state isn't captured. Consider reworking the UI so that visibility state is captured in data/code somewhere and the UI merely observes it.

        public void Toggle()
        {
            if (showing)
                Hide();
            else
                Show();
        }

        public void Show()
        {
            MyCanvasGroup.alpha = 1;
            MyCanvasGroup.interactable = true;
            MyCanvasGroup.blocksRaycasts = true;
            showing = true;
            m_onVisibilityChange?.Invoke(true);
        }

        public void Hide()
        {
            MyCanvasGroup.alpha = 0;
            MyCanvasGroup.interactable = false;
            MyCanvasGroup.blocksRaycasts = false;
            showing = false;
            m_onVisibilityChange?.Invoke(false);
        }

   
    }
}
