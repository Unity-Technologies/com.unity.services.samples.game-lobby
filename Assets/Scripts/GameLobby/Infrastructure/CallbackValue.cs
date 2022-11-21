using System;
using UnityEngine;

namespace LobbyRelaySample
{
    public class CallbackValue<T>
    {
        public Action<T> onChanged;


        public CallbackValue()
        {

        }
        public CallbackValue(T cachedValue)
        {
            m_CachedValue = cachedValue;
        }

        public T Value
        {
            get => m_CachedValue;
            set
            {
                if (m_CachedValue!=null&&m_CachedValue.Equals(value))
                    return;
                m_CachedValue = value;
                onChanged?.Invoke(m_CachedValue);
            }
        }

        public void ForceSet(T value)
        {
            m_CachedValue = value;
            onChanged?.Invoke(m_CachedValue);
        }

        public void SetNoCallback(T value)
        {
            m_CachedValue = value;
        }

        T m_CachedValue = default;
    }
}
