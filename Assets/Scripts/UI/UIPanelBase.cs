using UnityEngine;
using UnityEngine.Events;

namespace LobbyRelaySample.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class UIPanelBase : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent<bool> m_onVisibilityChange;
        [SerializeField]
        bool showing;

        CanvasGroup m_canvasGroup;

        protected CanvasGroup MyCanvasGroup
        {
            get
            {
                if (m_canvasGroup != null) return m_canvasGroup;
                return m_canvasGroup = GetComponent<CanvasGroup>();
            }
        }

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
