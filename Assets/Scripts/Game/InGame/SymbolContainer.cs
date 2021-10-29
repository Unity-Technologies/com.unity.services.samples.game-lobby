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
                // Note: The SymbolObjects, which will be children of this object, need their NetworkTransforms to have IsLocalSpace set to true. Otherwise, they might get desynced.
                // (This would manifest as packet loss errors.)
                // Also note: The initial position of the SymbolObject prefab is set to be outside the camera view in the Z-direction, so that it doesn't interpolate past the actual
                // position when it spawns on a client (as opposed to in the Y-direction, since this SymbolContainer is also moving downward).
                // TODO: Does that matter if we delay for the instructions?
                Rigidbody m_rb = this.GetComponent<Rigidbody>();
                m_rb.MovePosition(Vector3.up * 10);
                m_rb.velocity = Vector3.down;
            }
        }
    }
}
