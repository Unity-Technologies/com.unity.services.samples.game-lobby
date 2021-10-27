using Unity.Netcode;
using UnityEngine;

namespace LobbyRelaySample.inGame
{
    /// <summary>
    /// This holds the logic and data for an individual symbol, which can be "clicked" if the server detects the collision with a player who sends a click input.
    /// </summary>
    public class SymbolObject : NetworkBehaviour
    {
        [SerializeField] private SymbolData m_symbolData;
        [SerializeField] private SpriteRenderer m_renderer;
        [HideInInspector] public NetworkVariable<int> symbolIndex; // The index into SymbolData, not the index of this object.
        private ulong m_localId;

        public override void OnNetworkSpawn()
        {
            symbolIndex.OnValueChanged += OnSymbolIndexSet;
            m_localId = NetworkManager.Singleton.LocalClientId;
        }

        /// <summary>
        /// Because of the need to distinguish host vs. client calls, we use the symbolIndex NetworkVariable to learn what symbol to display.
        /// </summary>
        private void OnSymbolIndexSet(int prevValue, int newValue)
        {
            m_renderer.sprite = m_symbolData.GetSymbolForIndex(symbolIndex.Value);
            symbolIndex.OnValueChanged -= OnSymbolIndexSet;
        }

        /// <summary>
        /// The host has confirmed this symbol as a valid selection (this player's cursor collides with it and it's also next in their target sequence), so handle any visual feedback.
        /// </summary>
        [ClientRpc]
        public void OnSelectConfirmed_ClientRpc()
        {
            // TODO: Visual effects here.
        }

        [ServerRpc]
        public void Destroy_ServerRpc()
        {
            // Actually destroying the symbol objects can cause garbage collection and other delays that might lead to desyncs.
            // Instead, just deactivate the object, and it will be cleaned up once the NetworkManager is destroyed.
            // (If object pooling, this is where to instead return it to the pool.)
            this.transform.localPosition = Vector3.down * 500;
            // TODO: Visually disappear immediately.
        }
    }
}
