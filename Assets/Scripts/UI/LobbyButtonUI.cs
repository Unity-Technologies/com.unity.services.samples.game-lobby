using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace LobbyRelaySample.UI
{
    [RequireComponent(typeof(LocalLobbyObserver))]
    public class LobbyButtonUI : MonoBehaviour
    {
        [SerializeField]
        TMP_Text lobbyNameText;
        [SerializeField]
        TMP_Text lobbyCountText;

        /// <summary>
        /// Subscribed to on instantiation to pass our lobby data back
        /// </summary>
        public UnityEvent<LocalLobby> onLobbyPressed;
        LocalLobbyObserver m_DataObserver;

        void Awake()
        {
            m_DataObserver = GetComponent<LocalLobbyObserver>();
        }

        /// <summary>
        /// UI CallBack
        /// </summary>
        public void OnLobbyClicked()
        {
            onLobbyPressed?.Invoke(m_DataObserver.observed);
        }

        public void UpdateLobby(LocalLobby lobby)
        {
            m_DataObserver.observed.CopyObserved(lobby);
        }

        public void OnRoomUpdated(LocalLobby data)
        {
            lobbyNameText.SetText(data.LobbyName);
            lobbyCountText.SetText($"{data.PlayerCount}/{data.MaxPlayerCount}");
        }
    }
}
