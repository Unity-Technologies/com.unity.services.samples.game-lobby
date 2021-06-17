using System;
using LobbyRooms;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player
{
    /// <summary>
    /// Holds an instance of a lobbyplayer, and implements hooks for the UI to interact with.
    /// </summary>
    public class LobbyUserObserver : ObserverBehaviour<LobbyUser> { }
}
