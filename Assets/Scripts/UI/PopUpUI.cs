using LobbyRelaySample.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace LobbyRelaySample
{
    public class PopUpUI : MonoBehaviour
    {
        [SerializeField]
        TMP_InputField m_popupText;

        public void ShowPopup(string newText, Color textColor = default)
        {
            m_popupText.SetTextWithoutNotify(newText);
            m_popupText.textComponent.color = textColor;
        }

        public void Delete()
        {
            Destroy(gameObject);
        }
    }
}
