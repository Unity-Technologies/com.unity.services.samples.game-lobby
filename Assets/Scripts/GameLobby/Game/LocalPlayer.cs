using System;

namespace LobbyRelaySample
{
    /// <summary>
    /// Current state of the user in the lobby.
    /// This is a Flags enum to allow for the Inspector to select multiples for various UI features.
    /// </summary>
    [Flags]
    public enum UserStatus
    {
        None = 0,
        Connecting = 1, // User has joined a lobby but has not yet connected to Relay.
        Lobby = 2, // User is in a lobby and connected to Relay.
        Ready = 4, // User has selected the ready button, to ready for the "game" to start.
        InGame = 8, // User is part of a "game" that has started.
        Menu = 16 // User is not in a lobby, in one of the main menus.
    }

    /// <summary>
    /// Data for a local player instance. This will update data and is observed to know when to push local player changes to the entire lobby.
    /// </summary>
    [Serializable]
    public class LocalPlayer : Observed<LocalPlayer>
    {
        public CallbackValue<bool> IsHost = new CallbackValue<bool>(false);
        public CallbackValue<string> DisplayName = new CallbackValue<string>("");
        public CallbackValue<EmoteType> Emote = new CallbackValue<EmoteType>(EmoteType.None);
        public CallbackValue<UserStatus> UserStatus = new CallbackValue<UserStatus>((UserStatus)0);
        public CallbackValue<string> ID = new CallbackValue<string>("");

        public LocalPlayer(string id, bool isHost, string displayName,
            EmoteType emote = default, UserStatus status = default)
        {
            IsHost.Value = isHost;
            DisplayName.Value = displayName;
            Emote.Value = emote;
            UserStatus.Value = status;
            ID.Value = id;
        }

        public void ResetState()
        {
            IsHost.Value = false;
            Emote.Value = EmoteType.None;
            UserStatus.Value = LobbyRelaySample.UserStatus.Menu;
        }


        public override void CopyObserved(LocalPlayer observed)
        {
            OnChanged(this);
        }
    }
}
