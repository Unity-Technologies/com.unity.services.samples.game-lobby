using System.Collections.Generic;

namespace Unity.Services.Core
{
    /// <summary>
    /// Contain all options to customize services initialization when
    /// calling <see cref="UnityServices.Initialize(InitializationOptions)"/>.
    /// </summary>
    public class InitializationOptions
    {
        IDictionary<string, object> m_Values;

        internal IEnumerable<KeyValuePair<string, object>> Values => m_Values;

        /// <summary>
        /// Create a new instance of the <see cref="InitializationOptions"/> class.
        /// </summary>
        public InitializationOptions()
            : this(new Dictionary<string, object>()) {}

        internal InitializationOptions(IDictionary<string, object> values)
        {
            m_Values = values;
        }

        /// <summary>
        /// Get the option for the given <paramref name="key"/> if any.
        /// </summary>
        /// <param name="key">
        /// The key of the option to retrieve.
        /// </param>
        /// <param name="option">
        /// The stored option if any.
        /// </param>
        /// <returns>
        /// Return true if there is a bool for the given <paramref name="key"/>;
        /// return false otherwise.
        /// </returns>
        public bool TryGetOption(string key, out bool option)
        {
            return TryGetOption<bool>(key, out option);
        }

        /// <summary>
        /// Get the option for the given <paramref name="key"/> if any.
        /// </summary>
        /// <param name="key">
        /// The key of the option to retrieve.
        /// </param>
        /// <param name="option">
        /// The stored option if any.
        /// </param>
        /// <returns>
        /// Return true if there is a int for the given <paramref name="key"/>;
        /// return false otherwise.
        /// </returns>
        public bool TryGetOption(string key, out int option)
        {
            return TryGetOption<int>(key, out option);
        }

        /// <summary>
        /// Get the option for the given <paramref name="key"/> if any.
        /// </summary>
        /// <param name="key">
        /// The key of the option to retrieve.
        /// </param>
        /// <param name="option">
        /// The stored option if any.
        /// </param>
        /// <returns>
        /// Return true if there is a float for the given <paramref name="key"/>;
        /// return false otherwise.
        /// </returns>
        public bool TryGetOption(string key, out float option)
        {
            return TryGetOption<float>(key, out option);
        }

        /// <summary>
        /// Get the option for the given <paramref name="key"/> if any.
        /// </summary>
        /// <param name="key">
        /// The key of the option to retrieve.
        /// </param>
        /// <param name="option">
        /// The stored option if any.
        /// </param>
        /// <returns>
        /// Return true if there is a string for the given <paramref name="key"/>;
        /// return false otherwise.
        /// </returns>
        public bool TryGetOption(string key, out string option)
        {
            return TryGetOption<string>(key, out option);
        }

        bool TryGetOption<T>(string key, out T option)
        {
            option = default;

            if (m_Values.TryGetValue(key, out var rawValue)
                && rawValue is T value)
            {
                option = value;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Stores the given <paramref name="value"/> for the given <paramref name="key"/>.
        /// </summary>
        /// <param name="key">
        /// The identifier of the configuration entry.
        /// </param>
        /// <param name="value">
        /// The value to store.
        /// </param>
        /// <returns>
        /// Return this instance.
        /// </returns>
        public InitializationOptions SetOption(string key, bool value)
        {
            m_Values[key] = value;
            return this;
        }

        /// <summary>
        /// Stores the given <paramref name="value"/> for the given <paramref name="key"/>.
        /// </summary>
        /// <param name="key">
        /// The identifier of the configuration entry.
        /// </param>
        /// <param name="value">
        /// The value to store.
        /// </param>
        /// <returns>
        /// Return this instance.
        /// </returns>
        public InitializationOptions SetOption(string key, int value)
        {
            m_Values[key] = value;
            return this;
        }

        /// <summary>
        /// Stores the given <paramref name="value"/> for the given <paramref name="key"/>.
        /// </summary>
        /// <param name="key">
        /// The identifier of the configuration entry.
        /// </param>
        /// <param name="value">
        /// The value to store.
        /// </param>
        /// <returns>
        /// Return this instance.
        /// </returns>
        public InitializationOptions SetOption(string key, float value)
        {
            m_Values[key] = value;
            return this;
        }

        /// <summary>
        /// Stores the given <paramref name="value"/> for the given <paramref name="key"/>.
        /// </summary>
        /// <param name="key">
        /// The identifier of the configuration entry.
        /// </param>
        /// <param name="value">
        /// The value to store.
        /// </param>
        /// <returns>
        /// Return this instance.
        /// </returns>
        public InitializationOptions SetOption(string key, string value)
        {
            m_Values[key] = value;
            return this;
        }
    }
}
