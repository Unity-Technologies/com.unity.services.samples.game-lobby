using System;
using UnityEngine;

namespace GameLobby.UI
{
    //"Animates" the cursor when clicking
    public class CursorHandler : MonoBehaviour
    {
        [SerializeField]
        Texture2D m_defaultTexture;
        [SerializeField]
        Texture2D m_ClickedTexture;

        void Awake()
        {    
            Cursor.SetCursor(m_defaultTexture, Vector3.zero, CursorMode.Auto);
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.SetCursor(m_ClickedTexture, Vector3.zero, CursorMode.Auto);
            }

            if (Input.GetMouseButtonUp(0))
            {
                Cursor.SetCursor(m_defaultTexture, Vector3.zero, CursorMode.Auto);
            }
        }
    }
}
