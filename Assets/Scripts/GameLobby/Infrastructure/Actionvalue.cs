using System;
using UnityEngine;

namespace LobbyRelaySample
{
    public class ObservedValue<T>
    {
        public Action<T> onValueChanged;

        public T Value
        {
            get => m_CachedValue;
            set
            {
                if (m_CachedValue.Equals(value))
                    return;
                m_CachedValue = value;
                onValueChanged?.Invoke(m_CachedValue);
            }
        }

        T m_CachedValue = default;
    }
}