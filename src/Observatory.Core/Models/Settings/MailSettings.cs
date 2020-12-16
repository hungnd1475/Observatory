using Observatory.Core.Persistence;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text;

namespace Observatory.Core.Models.Settings
{
    public class MailSettings : ReactiveSettings
    {
        [Reactive]
        public MarkingAsReadBehavior MarkingAsReadBehavior { get; set; }

        [Reactive]
        public int MarkingAsReadWhenViewedSeconds { get; set; }

        public MailSettings(ISettingsStore store)
            : base(store, nameof(MailSettings))
        {
            MarkingAsReadBehavior = LoadProperty(nameof(MarkingAsReadBehavior), () => MarkingAsReadBehavior.WhenSelectionChanged);
            MarkingAsReadWhenViewedSeconds = LoadProperty(nameof(MarkingAsReadWhenViewedSeconds), () => 5);
        }
    }
}
