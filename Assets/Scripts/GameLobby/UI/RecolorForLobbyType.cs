using UnityEngine;
using UnityEngine.UI;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// We want to illustrate filtering the lobby list by some arbitrary variable. This will allow the lobby host to choose a color for the lobby, and will display a lobby's current color.
    /// (Note that this isn't sent over Relay to other clients for realtime updates.)
    /// </summary>
    public class RecolorForLobbyType : MonoBehaviour
    {
        static readonly Color s_orangeColor = new Color(0.8352942f, 0.3686275f, 0);
        static readonly Color s_greenColor = new Color(0, 0.6196079f, 0.4509804f);
        static readonly Color s_blueColor = new Color(0.0f, 0.4470589f, 0.6980392f);
        static readonly Color[] s_colorsOrdered = new Color[]
            { new Color(0.9f, 0.9f, 0.9f, 0.7f), s_orangeColor, s_greenColor, s_blueColor };

        [SerializeField]
        Graphic[] m_toRecolor;
        LocalLobby m_lobby;

        void Start()
        {
            m_lobby = GameManager.Instance.LocalLobby;
            m_lobby.LocalLobbyColor.onChanged += ChangeColors;
        }

        /// <summary>
        /// Called in-editor by toggles to set the color of the lobby.
        /// Triggers the ChangeColors method above
        /// </summary>
        public void SetLobbyColor(int color)
        {
            GameManager.Instance.SetLocalLobbyColor(color);
        }

        void ChangeColors(LobbyColor lobbyColor)
        {
            Color color = s_colorsOrdered[(int)lobbyColor];
            foreach (Graphic graphic in m_toRecolor)
                graphic.color = new Color(color.r, color.g, color.b, graphic.color.a);
        }
    }
}