using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Temporary script to allow a user to delete all the rooms they've previously made, so we don't have orphaned empty rooms.
/// </summary>
public class TempDeleteAllRooms : MonoBehaviour
{
    private Queue<Unity.Services.Rooms.Models.Room> m_pendingRooms;

    public void OnButton()
    {
        LobbyRooms.Rooms.RoomsInterface.QueryAllRoomsAsync((qr) => { DoDeletes(qr); });
    }

    private void DoDeletes(Unity.Services.Rooms.Response<Unity.Services.Rooms.Models.QueryResponse> response)
    {
        if (response != null && response.Status >= 200 && response.Status < 300)
        {
            StartCoroutine(DeleteCoroutine(response.Result.Results));
        }
    }

    private IEnumerator DeleteCoroutine(List<Unity.Services.Rooms.Models.Room> rooms)
    {
        foreach (var room in rooms)
        {
            LobbyRooms.Rooms.RoomsInterface.DeleteRoomAsync(room.Id, null); // The onComplete callback isn't called in some error cases, e.g. a 403 when we don't have permissions, so don't block on it.
            yield return new WaitForSeconds(1); // We need to wait a little to avoid 429's, but we might not run an onComplete depending on how the delete call fails.
        }
    }
}