using System;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Current user statea
    /// Set as a flag to allow for the unity inspector to select multiples for various UI features.
    /// </summary>
    [Flags]
    public enum UserPermission
    {
        Client = 1,
        Host = 2
    }

    /// <summary>
    /// Shows the UI when the lobbyuser is set to the matching conditions.
    /// </summary>
    [RequireComponent(typeof(LobbyUserObserver))]
    public class UserStateVisibilityUI : ObserverPanel<LobbyUser>
    {
        public UserStatus ShowThisWhen;
        public UserPermission Permissions;

        public override void ObservedUpdated(LobbyUser observed)
        {
            var hasStatusFlags = ShowThisWhen.HasFlag(observed.UserStatus);

            var hasPermissions = false;

            if (Permissions.HasFlag(UserPermission.Host) && observed.IsHost)
            {
                hasPermissions = true;
            }
            else if (Permissions.HasFlag(UserPermission.Client) && !observed.IsHost)
            {
                hasPermissions = true;
            }

            if (hasStatusFlags && hasPermissions)
                Show();
            else
                Hide();
        }
    }
}
