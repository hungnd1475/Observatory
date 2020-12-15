using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.ViewModels
{
    /// <summary>
    /// Defines a contract for view models shown in the main screen and routed by <see cref="MainViewModel"/>.
    /// </summary>
    public interface IFunctionalityViewModel : IRoutableViewModel
    {
        /// <summary>
        /// Gets or sets the instance of host screen used to route the view model.
        /// </summary>
        new MainViewModel HostScreen { get; set; }

        /// <summary>
        /// Called when the view model is navigated away.
        /// </summary>
        void OnNavigatedAway();

        /// <summary>
        /// Called when the view model is navigated to.
        /// </summary>
        void OnNavigatedTo();
    }
}
