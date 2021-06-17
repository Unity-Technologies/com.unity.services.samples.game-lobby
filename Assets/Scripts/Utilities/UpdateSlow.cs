using System.Collections.Generic;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Utilities
{
    public delegate void UpdateMethod(float dt);

    public interface IUpdateSlow : IProvidable<IUpdateSlow>
    {
        void OnUpdate(float dt);
        void Subscribe(UpdateMethod onUpdate);
        void Unsubscribe(UpdateMethod onUpdate);
    }

    /// <summary>
    /// A default implementation.
    /// </summary>
    public class UpdateSlowNoop : IUpdateSlow
    {
        public void OnUpdate(float dt) { }
        public void Subscribe(UpdateMethod onUpdate) { }
        public void Unsubscribe(UpdateMethod onUpdate) { }
        public void OnReProvided(IUpdateSlow prev) { }
    }

    /// <summary>
    /// Some objects might need to be on a slower update loop than the usual MonoBehaviour Update, e.g. to refresh data from services.
    /// Some might also not want to be coupled to a Unity object at all but still need an update loop.
    /// </summary>
    public class UpdateSlow : MonoBehaviour, IUpdateSlow
    {
        [SerializeField]
        private float m_updatePeriod = 1;
        [SerializeField]
        [Tooltip("If a subscriber to slow update takes longer than this to execute, it will be unsubscribed.")]
        private float m_durationToleranceMs = 10;
        private List<UpdateMethod> m_subscribers = new List<UpdateMethod>();
        private float m_updateTimer = 0;

        public void Awake()
        {
            Locator.Get.Provide(this);
        }
        public void OnDestroy()
        {
            // We should clean up references in case they would prevent garbage collection.
            m_subscribers.Clear();
        }

        /// <summary>Don't assume that onUpdate will be called in any particular order compared to other subscribers.</summary>
        public void Subscribe(UpdateMethod onUpdate)
        {
            if (!m_subscribers.Contains(onUpdate))
                m_subscribers.Add(onUpdate);
        }
        /// <summary>Safe to call even if onUpdate was not previously Subscribed.</summary>
        public void Unsubscribe(UpdateMethod onUpdate)
        {
            m_subscribers.Remove(onUpdate);
        }

        private void Update()
        {
            m_updateTimer += Time.deltaTime;
            if (m_updateTimer > m_updatePeriod)
            {
                float actualUpdate = ((int)(m_updateTimer / m_updatePeriod)) * m_updatePeriod; // If periods would overlap, aggregate them together.
                m_updateTimer -= actualUpdate;
                OnUpdate(actualUpdate);
            }
        }

        public void OnUpdate(float dt)
        {
            Stopwatch stopwatch = new Stopwatch();
            for (int sub = 0; sub < m_subscribers.Count; sub++)
            {
                UpdateMethod onUpdate = m_subscribers[sub];
                if (onUpdate == null || onUpdate.Target == null) // In case something forgets to Unsubscribe when it dies.
                {   Remove($"Did not Unsubscribe from UpdateSlow: {onUpdate.Target} : {onUpdate.Method}");
                    continue;
                }
                if (onUpdate.Method.ToString().Contains("<")) // Detect an anonymous or lambda or local method that cannot be Unsubscribed, by checking for a character that can't exist in a declared method name.
                {   Remove($"Removed anonymous from UpdateSlow: {onUpdate.Target} : {onUpdate.Method}");
                    continue;
                }

                stopwatch.Restart();
                onUpdate?.Invoke(dt);
                stopwatch.Stop();
                if (stopwatch.ElapsedMilliseconds > m_durationToleranceMs)
                    Remove($"UpdateSlow subscriber took too long, removing: {onUpdate.Target} : {onUpdate.Method}");
                
                void Remove(string msg)
                {
                    m_subscribers.RemoveAt(sub);
                    sub--;
                    Debug.LogError(msg);
                }
            }
        }

        public void OnReProvided(IUpdateSlow prevUpdateSlow)
        {
            if (prevUpdateSlow is UpdateSlow)
                m_subscribers.AddRange((prevUpdateSlow as UpdateSlow).m_subscribers);
        }
    }
}
