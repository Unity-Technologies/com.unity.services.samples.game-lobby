using Unity.Netcode;
using UnityEngine;

namespace LobbyRelaySample.inGame
{
    public class SymbolObject : NetworkBehaviour
    {
        public void OnSelect()
        {
            Destroy_ServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void Destroy_ServerRpc()
        {
            // Actually destroying the symbol objects can cause garbage collection and other delays that might lead to desyncs.
            // Instead, just deactivate the object, and it will be cleaned up once the NetworkManager is destroyed.
            // (If object pooling, this is where to instead return it to the pool.)
            this.transform.localPosition = Vector3.down * 500;
            // TODO: Visually disappear immediately.
        }
    }
}
