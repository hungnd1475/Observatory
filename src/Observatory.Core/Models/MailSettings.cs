using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Core.Models
{
    public class MailSettings : ReactiveObject, ISettings
    {
        [Reactive]
        public MarkAsReadBehavior MarkAsReadBehavior { get; set; }

        [Reactive]
        public int MarkAsReadAfterViewedSeconds { get; set; }

        public MailSettings()
        {
            MarkAsReadBehavior = MarkAsReadBehavior.WhenSelectionChanged;
        }

        public Task Restore()
        {
            throw new NotImplementedException();
        }
    }

    public enum MarkAsReadBehavior
    {
        WhenSelectionChanged,
        Never,
        WhenViewed
    }
}
