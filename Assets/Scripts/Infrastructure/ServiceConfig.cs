using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LobbyRooms
{
    [CreateAssetMenu(order = 0, fileName = "new service Config", menuName = "LobbyRooms/ServiceConfig")]
    public class ServiceConfig : ScriptableObject
    {
        public string targetBasePath;
    }
}
