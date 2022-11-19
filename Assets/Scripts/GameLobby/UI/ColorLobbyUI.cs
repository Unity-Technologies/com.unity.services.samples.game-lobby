using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// We want to illustrate filtering the lobby list by some arbitrary variable. This will allow the lobby host to choose a color for the lobby, and will display a lobby's current color.
    /// (Note that this isn't sent over Relay to other clients for realtime updates.)
    /// </summary>
    public class ColorLobbyUI : MonoBehaviour
    {
        public bool m_UseLocalLobby;
        static readonly Color s_orangeColor = new Color(0.83f, 0.36f, 0);
        static readonly Color s_greenColor = new Color(0, 0.61f, 0.45f);
        static readonly Color s_blueColor = new Color(0.0f, 0.44f, 0.69f);
        static readonly Color[] s_colorsOrdered = new Color[]
            { new Color(0.9f, 0.9f, 0.9f, 0.7f), s_orangeColor, s_greenColor, s_blueColor };

        [SerializeField]
        Graphic[] m_toRecolor;
        LocalLobby m_lobby;

        void Start()
        {
            if (m_UseLocalLobby)
                SetLobby(GameManager.Instance.LocalLobby);
        }

        public void SetLobby(LocalLobby lobby)
        {
            ChangeColors(lobby.LocalLobbyColor.Value);
            lobby.LocalLobbyColor.onChanged += ChangeColors;
        }

        public void ToggleWhite(bool toggle)
        {
            if (!toggle)
                return;
            GameManager.Instance.SetLocalLobbyColor(0);
        }

        public void ToggleOrange(bool toggle)
        {
            if (!toggle)
                return;
            GameManager.Instance.SetLocalLobbyColor(1);
        }

        public void ToggleGreen(bool toggle)
        {
            if (!toggle)
                return;
            GameManager.Instance.SetLocalLobbyColor(2);
        }

        public void ToggleBlue(bool toggle)
        {
            if (!toggle)
                return;
            GameManager.Instance.SetLocalLobbyColor(3);
        }

        void ChangeColors(LobbyColor lobbyColor)
        {
            Color color = s_colorsOrdered[(int)lobbyColor];
            foreach (Graphic graphic in m_toRecolor)
                graphic.color = new Color(color.r, color.g, color.b, graphic.color.a);
        }
    }
}