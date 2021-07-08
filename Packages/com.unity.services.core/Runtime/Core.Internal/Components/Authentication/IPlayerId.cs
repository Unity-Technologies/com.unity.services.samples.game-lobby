using System;
using Unity.Services.Core;

namespace Unity.Services.Authentication
{
    /// <summary>
    /// Contract for objects providing information with the player identification (PlayerID) for currently signed in player.
    /// </summary>
    public interface IPlayerId : IServiceComponent
    {
        /// <summary>
        /// The ID of the player.
        /// </summary>
        string PlayerId { get; }

        /// <summary>
        /// Event raised when the player id changed.
        /// </summary>
        event Action<string> PlayerIdChanged;
    }
}
