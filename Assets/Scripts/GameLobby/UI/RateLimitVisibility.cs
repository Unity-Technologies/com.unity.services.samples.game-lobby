using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Observes the Lobby request rate limits and changes the visibility of a UIPanelBase to suit.
    /// E.g. the refresh button on the Join menu should be inactive after a refresh for long enough to avoid the lobby query rate limit.
    /// </summary>
    public class RateLimitVisibility : MonoBehaviour
    {
        [SerializeField]
        UIPanelBase m_target;
        [SerializeField]
        float m_alphaWhenHidden = 0.5f;
        [SerializeField]
        LobbyAsyncRequests.RequestType m_requestType;

        private void Start()
        {
            LobbyAsyncRequests.Instance.GetRateLimit(m_requestType).onChanged += UpdateVisibility;
        }
        private void OnDestroy()
        {
            LobbyAsyncRequests.Instance.GetRateLimit(m_requestType).onChanged -= UpdateVisibility;
        }

        private void UpdateVisibility(LobbyAsyncRequests.RateLimitCooldown rateLimit)
        {
            if (rateLimit.IsInCooldown)
                m_target.Hide(m_alphaWhenHidden);
            else
                m_target.Show();
        }
    }
}
