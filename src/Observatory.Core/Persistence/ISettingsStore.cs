using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Persistence
{
    /// <summary>
    /// Defines a contract for a store that persists setting values.
    /// </summary>
    public interface ISettingsStore
    {
        /// <summary>
        /// Tries get a specific setting value given its key and the section its belongs to.
        /// </summary>
        /// <typeparam name="T">The type of the setting value.</typeparam>
        /// <param name="section">The name of the section the setting belongs to.</param>
        /// <param name="key">The key of the setting.</param>
        /// <param name="value">The value to be set if found in the store, otherwise default.</param>
        /// <returns>True if setting is found, false otherwise.</returns>
        bool TryGetEntry<T>(string section, string key, out T value);

        /// <summary>
        /// Persists a specific setting value to the store.
        /// </summary>
        /// <typeparam name="T">The type of the setting value.</typeparam>
        /// <param name="section">The name of the section the setting belongs to.</param>
        /// <param name="key">The key of the setting.</param>
        /// <param name="value">The value of the setting to be persisted.</param>
        void SetEntry<T>(string section, string key, T value);
    }
}
