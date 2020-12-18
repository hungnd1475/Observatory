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

        public ViewModelActivator Activator { get; } = new ViewModelActivator();

        public SettingsViewModel(IEnumerable<ISettingCategoryViewModel> categories)
        {
            Categories = categories;
        }
    }
}
