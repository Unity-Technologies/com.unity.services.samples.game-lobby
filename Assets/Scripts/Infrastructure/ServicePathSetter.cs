using System.Collections;
using System.Collections.Generic;
using LobbyRooms.Relay;
using LobbyRooms.Rooms;
using UnityEngine;

namespace LobbyRooms
{
    public class ServicePathSetter : MonoBehaviour
    {
        public ServiceConfig relayConfig;
        public ServiceConfig roomsConfig;

        void Awake()
        {
            RelayInterface.SetPath(relayConfig.targetBasePath);
            RoomsInterface.SetPath(roomsConfig.targetBasePath);
        }
    }
}
