using Unity.Netcode;
using UnityEngine;

namespace LobbyRelaySample.inGame
{
    [RequireComponent(typeof(Rigidbody))]
    public class SymbolContainer : NetworkBehaviour
    {
        public void Start()
        {
            if (IsHost)
                GetComponent<NetworkObject>().Spawn();
        }

        public override void OnNetworkSpawn()
        {
            if (IsHost)
            {
                Rigidbody m_rb = this.GetComponent<Rigidbody>();
                m_rb.MovePosition(Vector3.up * 10);
                m_rb.velocity = Vector3.down;
            }
        }
    }
}
