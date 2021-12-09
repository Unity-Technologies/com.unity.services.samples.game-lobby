using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace LobbyRelaySample.ngo
{
    /// <summary>
    /// This cursor object will follow the owning player's mouse cursor and be visible to the other players.
    /// The host will use this object's movement for detecting collision with symbol objects.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PlayerCursor : NetworkBehaviour, IReceiveMessages
    {
        [SerializeField] private SpriteRenderer m_renderer = default;
        [SerializeField] private ParticleSystem m_onClickParticles = default;
        [SerializeField] private TMPro.TMP_Text m_nameOutput = default;
        private Camera m_mainCamera;
        private NetworkVariable<Vector3> m_position = new NetworkVariable<Vector3>(NetworkVariableReadPermission.Everyone, Vector3.zero); // (Using a NetworkTransform to sync position would also work.)
        private ulong m_localId;

        // If the local player cursor spawns before this cursor's owner, the owner's data won't be available yet. This is used to retrieve the data later.
        private Action<ulong, Action<PlayerData>> m_retrieveName;

        // The host is responsible for determining if a player has successfully selected a symbol object, since collisions should be handled serverside.
        private List<SymbolObject> m_currentlyCollidingSymbols;

        public void Awake()
        {
            Locator.Get.Messenger.Subscribe(this);
        }

        /// <summary>
        /// This cursor is spawned in dynamically but needs references to some scene objects. Pushing full object references over RPC calls
        /// is an option if we create custom data for each and ensure they're all spawned on the host correctly, but it's simpler to do
        /// some one-time retrieval here instead.
        /// This also sets up the visuals to make remote player cursors appear distinct from the local player's cursor.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            m_retrieveName = NetworkedDataStore.Instance.GetPlayerData;
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
            else
            {   m_renderer.enabled = false; // The local player should see their cursor instead of the simulated cursor object, since the object will appear laggy.
            }
        }

        [ClientRpc]
        private void SetName_ClientRpc(PlayerData data)
        {
            if (!IsOwner)
                m_nameOutput.text = data.name;
        }

        // It'd be better to have a separate input handler, but we don't need the mouse input anywhere else, so keep it simple.
        private bool IsSelectInputHit()
        {
            return Input.GetMouseButtonDown(0);
        }

        public void Update()
        {
            transform.position = m_position.Value;
            if (m_mainCamera == null || !IsOwner)
                return;

            Vector3 targetPos = (Vector2)m_mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -m_mainCamera.transform.position.z));
            SetPosition_ServerRpc(targetPos); // Client can't set a network variable value.
            if (IsSelectInputHit())
                SendInput_ServerRpc(m_localId);
        }

        [ServerRpc] // Leave (RequireOwnership = true) for these so that only the player whose cursor this is can make updates.
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
            if (type == MessageType.MinigameBeginning)
            {
                m_retrieveName.Invoke(OwnerClientId, SetName_ClientRpc);
                Locator.Get.Messenger.Unsubscribe(this);
            }
        }
    }
}
