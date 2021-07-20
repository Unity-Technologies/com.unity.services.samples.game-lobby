namespace LobbyRelaySample
{
    public enum EmoteType { None = 0, Smile, Frown, Shock, Laugh }

    public static class EmoteTypeExtensions
    {
        public static string GetString(this EmoteType emote)
        {
            return
                emote == EmoteType.Smile ? ":D" :
                emote == EmoteType.Frown ? ":(" :
                emote == EmoteType.Shock ? ":O" :
                emote == EmoteType.Laugh ? "XD" :
                "";
        }
    }
}
