using System.Collections.Generic;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace LobbyRelaySample
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
        [Tooltip("Update interval. Note that lobby Get requests must occur at least 1 second apart, so this period should likely be greater than that.")]
        private float m_updatePeriod = 1.5f;
        [SerializeField]
        [Tooltip("If a subscriber to slow update takes longer than this to execute, it can be automatically unsubscribed.")]
        private float m_durationToleranceMs = 10;
        [SerializeField]
        [Tooltip("We ordinarily automatically remove a subscriber that takes too long. Otherwise, we'll simply log.")]
        private bool m_doNotRemoveIfTooLong = false;
        private List<UpdateMethod> m_subscribers = new List<UpdateMethod>();
        private float m_updateTimer = 0;
        private int m_nextActiveSubIndex = 0; // For staggering subscribers, to prevent spikes of lots of things triggering at once.

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
            int index = m_subscribers.IndexOf(onUpdate);
            if (index >= 0)
            {
                m_subscribers.Remove(onUpdate);
                if (index < m_nextActiveSubIndex)
                    m_nextActiveSubIndex--;
            }
        }

        private void Update()
        {
            if (m_subscribers.Count == 0)
                return;
            m_updateTimer += Time.deltaTime;
            float effectivePeriod = m_updatePeriod / m_subscribers.Count;
            while (m_updateTimer > effectivePeriod)
            {
                m_updateTimer -= effectivePeriod;
                OnUpdate(effectivePeriod);
            }
        }

        public void OnUpdate(float dt)
        {
            Stopwatch stopwatch = new Stopwatch();
            m_nextActiveSubIndex = System.Math.Max(0, System.Math.Min(m_subscribers.Count - 1, m_nextActiveSubIndex)); // Just a backup.
            UpdateMethod onUpdate = m_subscribers[m_nextActiveSubIndex];
            if (onUpdate == null || onUpdate.Target == null) // In case something forgets to Unsubscribe when it dies.
            {   Remove(m_nextActiveSubIndex, $"Did not Unsubscribe from UpdateSlow: {onUpdate.Target} : {onUpdate.Method}");
                return;
            }
            if (onUpdate.Method.ToString().Contains("<")) // Detect an anonymous or lambda or local method that cannot be Unsubscribed, by checking for a character that can't exist in a declared method name.
            {   Remove(m_nextActiveSubIndex, $"Removed anonymous from UpdateSlow: {onUpdate.Target} : {onUpdate.Method}");
                return;
            }

            stopwatch.Restart();
            onUpdate?.Invoke(dt);
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > m_durationToleranceMs)
            {
                if (!m_doNotRemoveIfTooLong)
                    Remove(m_nextActiveSubIndex, $"UpdateSlow subscriber took too long, removing: {onUpdate.Target} : {onUpdate.Method}");
                else
                {
                    Debug.LogWarning($"UpdateSlow subscriber took too long: {onUpdate.Target} : {onUpdate.Method}");
                    Increment();
                }
            }
            else
                Increment();

            void Remove(int index, string msg)
            {
                m_subscribers.RemoveAt(index);
                m_nextActiveSubIndex--;
                Debug.LogError(msg);
                Increment();
            }
            void Increment()
            {
                m_nextActiveSubIndex++;
                if (m_nextActiveSubIndex >= m_subscribers.Count)
                    m_nextActiveSubIndex = 0;
            }
        }

        public void OnReProvided(IUpdateSlow prevUpdateSlow)
        {
            if (prevUpdateSlow is UpdateSlow)
                m_subscribers.AddRange((prevUpdateSlow as UpdateSlow).m_subscribers);
        }
    }
}
