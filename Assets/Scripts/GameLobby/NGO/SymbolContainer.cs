using Unity.Netcode;
using UnityEngine;

namespace LobbyRelaySample.ngo
{
    /// <summary>
    /// Rather than track movement data for every symbol object, the symbols will all be parented under one container that will move.
    /// It will not begin that movement until it both has been Spawned on the network and it has been informed that the game has started.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SymbolContainer : NetworkBehaviour, IReceiveMessages
    {
        [SerializeField] private Rigidbody m_rb = default;
        [SerializeField] private float m_speed = 1;
        private bool m_isConnected = false;
        private bool m_hasGameStarted = false;
        /// <summary>
        /// Verify both that the game has started and that the network connection is working before moving the symbols.
        /// </summary>
        private void OnGameStarted()
        {
            m_hasGameStarted = true;
            if (m_isConnected)
                BeginMotion();
        }

        public void Awake() // If there's just one player, Start would occur after the GameBeginning message is sent, so use Awake/OnEnable instead.
        {
            Locator.Get.Messenger.Subscribe(this);
        }

        public void Start()
        {
            if (!IsHost)
            {   this.enabled = false; // Just disabling this script, not the whole GameObject.
                return;
            }
            GetComponent<NetworkObject>().Spawn();
        }

        public override void OnNetworkSpawn()
        {
            if (IsHost)
            {
                m_isConnected = true;
                m_rb.MovePosition(Vector3.up * 10);
                if (m_hasGameStarted)
                    BeginMotion();
            }
        }

        private void BeginMotion()
        {
            m_rb.velocity = Vector3.down * m_speed;
        }

        public void OnReceiveMessage(MessageType type, object msg)
        {
            if (type == MessageType.InstructionsShown)
            {   Locator.Get.Messenger.Unsubscribe(this);
                OnGameStarted();
            }
        }
    }
}
