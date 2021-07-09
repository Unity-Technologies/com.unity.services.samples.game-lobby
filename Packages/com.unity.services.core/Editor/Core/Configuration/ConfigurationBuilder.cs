using System.Collections.Generic;
using System.Globalization;

namespace Unity.Services.Core.Configuration.Editor
{
    /// <summary>
    /// Container for configuration values that need to be passed to
    /// the <see cref="IProjectConfiguration"/> component at runtime.
    /// </summary>
    public class ConfigurationBuilder
    {
        internal IDictionary<string, ConfigurationEntry> Values { get; }

        internal ConfigurationBuilder()
            : this(new Dictionary<string, ConfigurationEntry>()) {}

        internal ConfigurationBuilder(IDictionary<string, ConfigurationEntry> values)
        {
            Values = values;
        }

        /// <summary>
        /// Stores the given <paramref name="value"/> for the given <paramref name="key"/>.
        /// </summary>
        /// <param name="key">
        /// The identifier of the configuration entry.
        /// </param>
        /// <param name="value">
        /// The value to store.
        /// It is stored as a string using <see cref="CultureInfo.InvariantCulture"/>.
        /// </param>
        /// <param name="isReadOnly">
        /// Set to true to forbid game developers to override this setting.
        /// </param>
        /// <returns>
        /// Return this instance.
        /// </returns>
        public ConfigurationBuilder SetBool(string key, bool value, bool isReadOnly = false)
        {
            Values[key] = new ConfigurationEntry(value.ToString(CultureInfo.InvariantCulture), isReadOnly);
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
        /// It is stored as a string.
        /// </param>
        /// <param name="isReadOnly">
        /// Set to true to forbid game developers to override this setting.
        /// </param>
        /// <returns>
        /// Return this instance.
        /// </returns>
        public ConfigurationBuilder SetInt(string key, int value, bool isReadOnly = false)
        {
            Values[key] = new ConfigurationEntry(value.ToString(), isReadOnly);
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
        /// It is stored as a string using <see cref="CultureInfo.InvariantCulture"/>.
        /// </param>
        /// <param name="isReadOnly">
        /// Set to true to forbid game developers to override this setting.
        /// </param>
        /// <returns>
        /// Return this instance.
        /// </returns>
        public ConfigurationBuilder SetFloat(string key, float value, bool isReadOnly = false)
        {
            Values[key] = new ConfigurationEntry(value.ToString(CultureInfo.InvariantCulture), isReadOnly);
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
        /// <param name="isReadOnly">
        /// Set to true to forbid game developers to override this setting.
        /// </param>
        /// <returns>
        /// Return this instance.
        /// </returns>
        public ConfigurationBuilder SetString(string key, string value, bool isReadOnly = false)
        {
            Values[key] = new ConfigurationEntry(value, isReadOnly);
            return this;
        }
    }
}
