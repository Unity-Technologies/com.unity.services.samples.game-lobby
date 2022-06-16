using System;
using UnityEngine;

namespace LobbyRelaySample
{
    public class CallbackValue<T>
    {
        public Action<T> onChanged;

        public T Value
        {
            get => m_CachedValue;
            set
            {
                if (m_CachedValue.Equals(value))
                    return;
                m_CachedValue = value;
                onChanged?.Invoke(m_CachedValue);
            }
        }

        T m_CachedValue = default;
    }
}