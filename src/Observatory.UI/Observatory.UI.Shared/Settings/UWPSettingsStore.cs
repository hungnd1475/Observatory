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

        public void SetEntry<T>(string section, string key, T value)
        {
            var composite = _settings.Values.ContainsKey(section)
                ? (ApplicationDataCompositeValue)_settings.Values[section]
                : new ApplicationDataCompositeValue();
            composite[key] = value;
            _settings.Values[section] = composite;
        }

        public bool TryGetEntry<T>(string section, string key, out T value)
        {
            if (_settings.Values.ContainsKey(section))
            {
                var composite = (ApplicationDataCompositeValue)_settings.Values[section];
                if (composite.ContainsKey(key))
                {
                    value = (T)composite[key];
                    return true;
                }
            }
            value = default;
            return false;
        }
    }
}
