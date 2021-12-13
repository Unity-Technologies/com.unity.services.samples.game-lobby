using System;
using UnityEngine;

namespace LobbyRelaySample.ngo
{
    /// <summary>
    /// Used by the host to deactivate symbol objects once they're off-screen.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SymbolKillVolume : MonoBehaviour
    {
        private bool m_isInitialized = false;
        private Action m_onSymbolCollided;

        public void Initialize(Action onSymbolCollided)
        {
            m_onSymbolCollided = onSymbolCollided;
            m_isInitialized = true;
        }

        public void OnTriggerEnter(Collider other)
        {
            if (!m_isInitialized)
                return;

            SymbolObject symbolObj = other.GetComponent<SymbolObject>();
            if (symbolObj != null)
            {
                symbolObj.HideSymbol_ServerRpc();
                m_onSymbolCollided?.Invoke();
            }
        }
    }
}
