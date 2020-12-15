using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;

namespace Observatory.Core.ViewModels.Calendar
{
    public class CalendarViewModel : ReactiveObject, IFunctionalityViewModel
    {
        public string UrlPathSegment { get; } = "calendar";

        IScreen IRoutableViewModel.HostScreen => HostScreen;

        public MainViewModel HostScreen { get; set; }

        public void OnNavigatedAway()
        {
        }

        public void OnNavigatedTo()
        {
        }
    }
}
