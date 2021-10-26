using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace LobbyRelaySample.inGame
{
    /// <summary>
    /// Each player's cursor needs to be controlled by them and visible to the other players.
    /// </summary>
    public class PlayerCursor : NetworkBehaviour
    {
        private Camera m_mainCamera;
        private NetworkVariable<Vector3> m_position = new NetworkVariable<Vector3>(NetworkVariableReadPermission.Everyone, Vector3.zero);

        // The host is responsible for determining if a player has successfully selected a symbol object, since collisions should be handled serverside.
        private List<SymbolObject> m_currentlyCollidingSymbols;

        // We can't pass object references as RPC calls by default, and we don't have a different convenient way to, I think,
        // get the object spawned on the server to assign some member on the client, so instead let's retrieve dynamically what we need.
        // I guess I'd just have a "singleton" to hold the references?
        public override void OnNetworkSpawn()
        {
            m_mainCamera = GameObject.Find("InGameCamera").GetComponent<Camera>();
            if (IsHost)
                m_currentlyCollidingSymbols = new List<SymbolObject>();
        }

        // Don't love having the input here, but it doesn't need to be anywhere else.
        private bool IsSelectInputHit()
        {
            return Input.GetMouseButtonDown(0);
        }

        public void Update() // TODO: FixedUpdate?
        {
            transform.position = m_position.Value;
            if (m_mainCamera == null || !IsOwner)
                return;

            Vector3 targetPos = (Vector2)m_mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -m_mainCamera.transform.position.z));
            SetPosition_ServerRpc(targetPos); // Client can't set a network variable value.
            if (IsSelectInputHit())
                SendInput_ServerRpc();
        }

        [ServerRpc] // Leave RequireOwnership = true for these so that only the player whose cursor this is can make updates.
        private void SetPosition_ServerRpc(Vector3 position)
        {
            m_position.Value = position;
        }

        [ServerRpc]
        private void SendInput_ServerRpc()
        {
            if (m_currentlyCollidingSymbols.Count > 0)
            {
                SymbolObject symbol = m_currentlyCollidingSymbols[0];
                m_currentlyCollidingSymbols.RemoveAt(0);
                symbol.OnSelect();
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            if (!IsHost)
                return;
            SymbolObject symbol = other.GetComponent<SymbolObject>();
            if (symbol == null)
                return;
            if (!m_currentlyCollidingSymbols.Contains(symbol))
                m_currentlyCollidingSymbols.Add(symbol);
        }

        public void OnTriggerExit(Collider other)
        {
            if (!IsHost)
                return;
            SymbolObject symbol = other.GetComponent<SymbolObject>();
            if (symbol == null)
                return;
            if (m_currentlyCollidingSymbols.Contains(symbol))
                m_currentlyCollidingSymbols.Remove(symbol);
        }
    }
}
