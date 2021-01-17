using Observatory.Core.Persistence;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.Storage;

namespace Observatory.UI.Settings
{
    public class UWPSettingsStore : ISettingsStore
    {
        private readonly ApplicationDataContainer _settings;

        public UWPSettingsStore()
        {
            _settings = ApplicationData.Current.LocalSettings;
        }

        public void SetEntry<T>(string className, string propertyName, T value)
        {
            var key = $"{className}.{propertyName}";
            _settings.Values[key] = value;
        }

        public bool TryGetEntry<T>(string className, string propertyName, out T value)
        {
            var key = $"{className}.{propertyName}";
            if (_settings.Values.ContainsKey(key))
            {
                value = (T)_settings.Values[key];
                return true;
            }
            value = default;
            return false;
        }
    }
}
