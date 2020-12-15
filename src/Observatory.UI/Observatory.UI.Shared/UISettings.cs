using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Observatory.Core.Models;

namespace Observatory.UI
{
    public class UISettings : ReactiveObject, ISettings
    {
        [Reactive]
        public double NavigationViewOpenPaneLength { get; set; } = 300;

        [Reactive]
        public bool IsNavigationViewPaneOpen { get; set; } = true;
        
        [Reactive]
        public double MessageListWidth { get; set; } = 500;

        public UISettings()
        {
        }

        public Task Restore()
        {
            throw new NotImplementedException();
        }
    }
}
