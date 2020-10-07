using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;

namespace Observatory.Core.ViewModels.Calendar
{
    public class CalendarViewModel : ReactiveObject, IRoutableViewModel
    {
        public string UrlPathSegment { get; } = "calendar";

        public IScreen HostScreen { get; set; }

        public MainViewModel Main => (MainViewModel)HostScreen;
    }
}
