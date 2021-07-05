using System;

namespace LobbyRelaySample
{
    /// <summary>
    /// In your Observed children, be sure to call OnChanged when setting the value of any property.
    /// </summary>
    /// <typeparam name="T">The Data we want to view.</typeparam>
    public abstract class Observed<T>
    {
        /// <summary>
        /// If you want to copy all of the values, and only trigger OnChanged once.
        /// </summary>
        /// <param name="oldObserved"></param>
        public abstract void CopyObserved(T oldObserved);

        public Action<T> onChanged { get; set; }
        public Action<T> onDestroyed { get; set; }

        /// <summary>
        /// Should be implemented into every public property of the observed 
        /// </summary>
        /// <param name="observed">Instance of the observed that changed.</param>
        protected void OnChanged(T observed)
        {
            onChanged?.Invoke(observed);
        }

        protected void OnDestroy(T observed)
        {
            onDestroyed?.Invoke(observed);
        }
    }
}
