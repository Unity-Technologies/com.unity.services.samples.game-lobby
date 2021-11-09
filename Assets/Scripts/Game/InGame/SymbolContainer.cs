using Unity.Netcode;
using UnityEngine;

namespace LobbyRelaySample.inGame
{
    [RequireComponent(typeof(Rigidbody))]
    public class SymbolContainer : NetworkBehaviour, IReceiveMessages
    {
        [SerializeField] private Rigidbody m_rb = default;
        private bool m_isConnected = false;
        private bool m_hasGameStarted = false;
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
            {   this.enabled = false; // Just disabling this script.
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
            // Note: The SymbolObjects, which will be children of this object, need their NetworkTransforms to have IsLocalSpace set to true. Otherwise, they might get desynced.
            // (This would manifest as packet loss errors.)
            // Also note: The initial position of the SymbolObject prefab is set to be outside the camera view in the Z-direction, so that it doesn't interpolate past the actual
            // position when it spawns on a client (as opposed to in the Y-direction, since this SymbolContainer is also moving downward).
            m_rb.velocity = Vector3.down;
        }

        public void OnReceiveMessage(MessageType type, object msg)
        {
            if (type == MessageType.GameBeginning)
            {   Locator.Get.Messenger.Unsubscribe(this);
                OnGameStarted();
            }
        }
    }
}
