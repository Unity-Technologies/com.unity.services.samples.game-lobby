using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace LobbyRelaySample.ngo
{
    /// <summary>
    /// Each player's cursor needs to be controlled by them and visible to the other players.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PlayerCursor : NetworkBehaviour, IReceiveMessages
    {
        [SerializeField] private SpriteRenderer m_renderer = default;
        [SerializeField] private ParticleSystem m_onClickParticles = default;
        [SerializeField] private TMPro.TMP_Text m_nameOutput = default;
        private Camera m_mainCamera;
        private NetworkVariable<Vector3> m_position = new NetworkVariable<Vector3>(NetworkVariableReadPermission.Everyone, Vector3.zero);
        private ulong m_localId;
        private Action<ulong, Action<string>> m_retrieveName;

        // The host is responsible for determining if a player has successfully selected a symbol object, since collisions should be handled serverside.
        private List<SymbolObject> m_currentlyCollidingSymbols;

        public void Awake()
        {
            Locator.Get.Messenger.Subscribe(this);
        }

        // We can't pass object references as RPC calls by default, and we don't have a different convenient way to, I think,
        // get the object spawned on the server to assign some member on the client, so instead let's retrieve dynamically what we need.
        // I guess I'd just have a "singleton" to hold the references?
        public override void OnNetworkSpawn()
        {
            m_retrieveName = NetworkedDataStore.Instance.GetPlayerName;
            m_mainCamera = GameObject.Find("InGameCamera").GetComponent<Camera>();
            if (IsHost)
                m_currentlyCollidingSymbols = new List<SymbolObject>();
            m_localId = NetworkManager.Singleton.LocalClientId;
            // Other players' cursors should be less prominent than the local player's cursor.
            if (OwnerClientId != m_localId)
            {   m_renderer.transform.localScale *= 0.75f;
                m_renderer.color = new Color(1, 1, 1, 0.5f);
                var trails = m_onClickParticles.trails;
                trails.colorOverLifetime = new ParticleSystem.MinMaxGradient(Color.grey);
            }
        }

        [ClientRpc]
        private void SetName_ClientRpc(string name)
        {
            if (!IsOwner)
                m_nameOutput.text = name;
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
                SendInput_ServerRpc(m_localId);
        }

        [ServerRpc] // Leave RequireOwnership = true for these so that only the player whose cursor this is can make updates.
        private void SetPosition_ServerRpc(Vector3 position)
        {
            m_position.Value = position;
        }

        [ServerRpc]
        private void SendInput_ServerRpc(ulong id)
        {
            if (m_currentlyCollidingSymbols.Count > 0)
            {
                SymbolObject symbol = m_currentlyCollidingSymbols[0];
                Locator.Get.InGameInputHandler.OnPlayerInput(id, symbol);
            }
            OnInputVisuals_ClientRpc();
        }

        [ClientRpc]
        private void OnInputVisuals_ClientRpc()
        {
            m_onClickParticles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            m_onClickParticles.Play();
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

        public void OnReceiveMessage(MessageType type, object msg)
        {
            if (type == MessageType.GameBeginning)
            {
                m_retrieveName.Invoke(OwnerClientId, SetName_ClientRpc);
                Locator.Get.Messenger.Unsubscribe(this);
            }
        }
    }
}
