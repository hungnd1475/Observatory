using Observatory.Core.Persistence;
using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace Observatory.Core.Models.Settings
{
    /// <summary>
    /// Represents a base class for application settings.
    /// </summary>
    public abstract class ReactiveSettings : ReactiveObject
    {
        protected readonly ISettingsStore _store;
        protected readonly string _section;

        /// <summary>
        /// Constructs an instance of <see cref="ReactiveSettings"/>.
        /// </summary>
        /// <param name="store">The store where setting values are persisted.</param>
        /// <param name="section">The name of the settings section in the store.</param>
        /// <param name="throttlingInverval">The time to wait before persisting when received changes.</param>
        public ReactiveSettings(ISettingsStore store, string section,
            TimeSpan? throttlingInverval = null)
        {
            _store = store;
            _section = section;

            Changed.Throttle(throttlingInverval ?? TimeSpan.FromMilliseconds(100))
                .Subscribe(e =>
                {
                    var value = e.Sender.GetType().GetProperty(e.PropertyName).GetValue(e.Sender);
                    _store.SetEntry(section, e.PropertyName, value);
                });
        }

        /// <summary>
        /// Reads the value from the store for a given property. Inherited classes should call this
        /// method in their constructor to initialize the properties.
        /// </summary>
        /// <typeparam name="T">The type of the setting value.</typeparam>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="defaultFactory">The factory function to produce the default value in case the property is not persisted yet.</param>
        /// <returns></returns>
        protected virtual T LoadProperty<T>(string propertyName, Func<T> defaultFactory)
        {
            if (!_store.TryGetEntry<T>(_section, propertyName, out var value))
            {
                value = defaultFactory();
            }
            return value;
        }
    }
}
