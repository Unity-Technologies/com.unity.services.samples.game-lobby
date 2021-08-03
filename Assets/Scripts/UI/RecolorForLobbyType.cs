using UnityEngine;
using UnityEngine.UI;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// We want to illustrate filtering the lobby list by some arbitrary variable. This will allow the lobby host to choose a color for the lobby, and will display a lobby's current color.
    /// (Note that this isn't sent over Relay to other clients for realtime updates.)
    /// </summary>
    [RequireComponent(typeof(LocalLobbyObserver))]
    public class RecolorForLobbyType : MonoBehaviour
    {
        private static readonly Color s_orangeColor = new Color(0.75f, 0.5f, 0.1f);
        private static readonly Color s_greenColor  = new Color(0.5f, 1, 0.7f);
        private static readonly Color s_blueColor   = new Color(0.75f, 0.7f, 1);
        private static readonly Color[] s_colorsOrdered = new Color[] { Color.white, s_orangeColor, s_greenColor, s_blueColor };

        [SerializeField]
        private Graphic[] m_toRecolor;
        private LocalLobby m_lobby;

        public void UpdateLobby(LocalLobby lobby)
        {
            m_lobby = lobby;
            Color color = s_colorsOrdered[(int)lobby.Color];
            foreach (Graphic graphic in m_toRecolor)
                graphic.color = new Color(color.r, color.g, color.b, graphic.color.a);
        }

        /// <summary>
        /// Called in-editor by toggles to set the color of the lobby.
        /// </summary>
        public void ChangeColor(int color)
        {
            if (m_lobby != null)
                m_lobby.Color = (LobbyColor)color;
        }
    }
}
