using Observatory.Core.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.ViewModels.Settings
{
    public class AccountManagementViewModel : ReactiveObject, ISettingCategoryViewModel
    {
        public string Name { get; } = "Manage accounts";

        public AccountManagementViewModel()
        {
        }
    }
}
