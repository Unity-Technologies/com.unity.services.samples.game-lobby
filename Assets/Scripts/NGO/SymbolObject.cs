using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace LobbyRelaySample.ngo
{
    /// <summary>
    /// This holds the logic and data for an individual symbol, which can be "clicked" if the server detects the collision with a player who sends a click input.
    /// </summary>
    public class SymbolObject : NetworkBehaviour
    {
        [SerializeField]  private SymbolData m_symbolData;
        [SerializeField]  private SpriteRenderer m_renderer;
        [SerializeField]  private Animator m_animator;

        public bool Clicked { get; private set; }
        [HideInInspector] public NetworkVariable<int> symbolIndex; // The index into SymbolData, not the index of this object.

        public override void OnNetworkSpawn()
        {
            symbolIndex.OnValueChanged += OnSymbolIndexSet;
        }

        /// <summary>
        /// Because of the need to distinguish host vs. client calls, we use the symbolIndex NetworkVariable to learn what symbol to display.
        /// </summary>
        private void OnSymbolIndexSet(int prevValue, int newValue)
        {
            m_renderer.sprite = m_symbolData.GetSymbolForIndex(symbolIndex.Value);
            symbolIndex.OnValueChanged -= OnSymbolIndexSet;
        }

        [ServerRpc]
        public void ClickedSequence_ServerRpc(ulong clickerPlayerId)
        {
            Clicked = true;
            Clicked_ClientRpc(clickerPlayerId);
            StartCoroutine(HideSymbolAnimDelay());
        }

        [ClientRpc]
        public void Clicked_ClientRpc(ulong clickerPlayerId)
        {
            if (this.NetworkManager.LocalClientId == clickerPlayerId)
                m_animator.SetTrigger("iClicked");
            else
            {
                m_animator.SetTrigger("theyClicked");
            }
        }

        [ServerRpc]
        public void HideSymbol_ServerRpc()
        {
            // Actually destroying the symbol objects can cause garbage collection and other delays that might lead to desyncs.
            // Disabling the networked object can also cause issues, so instead, just move the object, and it will be cleaned up once the NetworkManager is destroyed.
            // (If we used object pooling, this is where we would instead return it to the pool.)
            //The animation calls RemoveSymbol(only for server
            this.transform.localPosition += Vector3.forward * 500;
        }

        //It's easier to have the post-animation symbol "deletion" happen entirely in server world rather than depend on client-side animation triggers.
        IEnumerator HideSymbolAnimDelay()
        {
            yield return new WaitForSeconds(0.3f);
            HideSymbol_ServerRpc();
        }

    }
}
