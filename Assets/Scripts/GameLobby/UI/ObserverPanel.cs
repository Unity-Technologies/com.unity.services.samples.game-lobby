namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Observer UI panel base class. This allows UI elements to be shown or hidden based on an Observed element.
    /// </summary>
    public abstract class ObserverPanel<T> : UIPanelBase where T : Observed<T>
    {
        public abstract void ObservedUpdated(T observed);
    }
}
