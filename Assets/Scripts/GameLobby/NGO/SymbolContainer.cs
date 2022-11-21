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
    public class SymbolContainer : NetworkBehaviour
    {
        [SerializeField]
        float m_speed = 1;
        bool m_isConnected = false;
        bool m_hasGameStarted = false;

        /// <summary>
        /// Verify both that the game has started and that the network connection is working before moving the symbols.
        /// </summary>
        public void StartMovingSymbols()
        {
            m_hasGameStarted = true;
            if (m_isConnected)
                BeginMotion();
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
            transform.position += Time.deltaTime * m_speed * Vector3.down;
        }
    }
}