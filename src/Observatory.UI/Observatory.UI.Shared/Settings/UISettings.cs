using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Observatory.Core.Models;
using Observatory.Core.Persistence;
using Observatory.Core.Models.Settings;
using Splat;
using System.Runtime.Serialization;

namespace Observatory.UI.Settings
{
    public class UISettings : ReactiveSettings
    {
        [Reactive]
        public double NavigationViewOpenPaneLength { get; set; }

        [Reactive]
        public bool IsNavigationViewPaneOpen { get; set; }

        [Reactive]
        public double MessageListWidth { get; set; }

        public UISettings(ISettingsStore store)
            : base(store, nameof(UISettings))
        {
            NavigationViewOpenPaneLength = LoadProperty(nameof(NavigationViewOpenPaneLength), () => 300d);
            IsNavigationViewPaneOpen = LoadProperty(nameof(IsNavigationViewPaneOpen), () => true);
            MessageListWidth = LoadProperty(nameof(MessageListWidth), () => 500d);
        }
    }
}
