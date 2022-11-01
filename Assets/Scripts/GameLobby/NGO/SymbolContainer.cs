using System;
using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Components;

namespace LobbyRelaySample.ngo
{
    /// <summary>
    /// Rather than track movement data for every symbol object, the symbols will all be parented under one container that will move.
    /// It will not begin that movement until it both has been Spawned on the network and it has been informed that the game has started.
    /// </summary>
    [RequireComponent(typeof(NetworkTransform))]
    public class SymbolContainer : NetworkBehaviour, IReceiveMessages
    {
        [SerializeField]
        float m_speed = 1;
        bool m_isConnected = false;
        bool m_hasGameStarted = false;

        /// <summary>
        /// Verify both that the game has started and that the network connection is working before moving the symbols.
        /// </summary>
        void OnGameStarted()
        {
            m_hasGameStarted = true;
            if (m_isConnected)
                BeginMotion();
        }

        public void Awake() // If there's just one player, Start would occur after the GameBeginning message is sent, so use Awake/OnEnable instead.
        {
            Locator.Get.Messenger.Subscribe(this);
        }


        public override void OnNetworkSpawn()
        {
            if (IsHost)
            {
                m_isConnected = true;
                transform.position = Vector3.up * 10;
            }
            else
            {
                this.enabled = false; // Just disabling this script, not the whole GameObject.
            }
        }

        void Update()
        {
            if (!IsHost)
                return;
            if (!m_hasGameStarted)
                return;
            BeginMotion();
        }

        void BeginMotion()
        {
            transform.position += Time.deltaTime * m_speed*Vector3.down;
        }

        public void OnReceiveMessage(MessageType type, object msg)
        {
            if (type == MessageType.InstructionsShown)
            {
                Locator.Get.Messenger.Unsubscribe(this);
                OnGameStarted();
            }
        }
    }
}
