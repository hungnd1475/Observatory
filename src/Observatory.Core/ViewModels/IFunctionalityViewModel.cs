using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.ViewModels
{
    /// <summary>
    /// Defines a contract for view models shown in the main screen and routed by <see cref="MainViewModel"/>.
    /// </summary>
    public interface IFunctionalityViewModel : IRoutableViewModel, IActivatableViewModel
    {
        /// <summary>
        /// Gets or sets the instance of host screen used to route the view model.
        /// </summary>
        new MainViewModel HostScreen { get; set; }
    }
}
