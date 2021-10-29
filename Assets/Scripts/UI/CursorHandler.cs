using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameLobby.UI
{
    //"Animates" the cursor when clicking
    public class CursorHandler : MonoBehaviour
    {
        [SerializeField]
        Texture2D m_defaultTexture;
        [SerializeField]
        Texture2D m_ClickedTexture;
        [SerializeField]
        GameObject m_ClickVfxPrefab;

        void Awake()
        {    
            Cursor.SetCursor(m_defaultTexture, Vector3.zero, CursorMode.Auto);
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.SetCursor(m_ClickedTexture, Vector3.zero, CursorMode.Auto);
                SpawnClickEffect();
            }

            if (Input.GetMouseButtonUp(0))
            {
                Cursor.SetCursor(m_defaultTexture, Vector3.zero, CursorMode.Auto);
            }
        }

        void SpawnClickEffect()
        {
            var screenLocation = Camera.current.ScreenToWorldPoint(Input.mousePosition+Camera.current.transform.forward*10);
            var clickVfxInstance = Instantiate(m_ClickVfxPrefab, screenLocation, Quaternion.identity);
            Destroy(clickVfxInstance, 1);
        }
    }
}
