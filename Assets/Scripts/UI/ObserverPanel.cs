namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Observer UI panel base class, for UI panels that need hiding, and hookup to observerBehaviours
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ObserverPanel<T> : UIPanelBase where T : Observed<T>
    {
        public abstract void ObservedUpdated(T observed);
       
    }

}
