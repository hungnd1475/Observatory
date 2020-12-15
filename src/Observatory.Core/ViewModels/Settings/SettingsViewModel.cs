using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.ViewModels.Settings
{
    public class SettingsViewModel : ReactiveObject, IFunctionalityViewModel
    {
        public string UrlPathSegment => "settings";

        IScreen IRoutableViewModel.HostScreen => HostScreen;

        public MainViewModel HostScreen { get; set; }

        public IEnumerable<ISettingCategoryViewModel> Categories { get; }

        public SettingsViewModel(IEnumerable<ISettingCategoryViewModel> categories)
        {
            Categories = categories;
        }

        public void OnNavigatedAway()
        {
        }

        public void OnNavigatedTo()
        {
        }
    }
}
