using System;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// User permission type. It's a flag enum to allow for the Inspector to select multiples for various UI features.
    /// </summary>
    [Flags]
    public enum UserPermission
    {
        Client = 1,
        Host = 2
    }

    /// <summary>
    /// Shows the UI when the LobbyUser matches some conditions, including having the target permissions.
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
