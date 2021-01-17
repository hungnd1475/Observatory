using DynamicData;
using Observatory.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Observatory.Core.Services
{
    /// <summary>
    /// Defines a contract for a service that handles profiles registration.
    /// </summary>
    public interface IProfileRegistrationService
    {
        /// <summary>
        /// Connects to the changes in profile registers.
        /// </summary>
        /// <returns></returns>
        IObservable<IChangeSet<ProfileRegister>> Connect();

        /// <summary>
        /// Initializes the service, creating the registry file or loading existing profiles.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Registers a new profile.
        /// </summary>
        /// <param name="profile">The profile to be registered.</param>
        /// <returns></returns>
        Task RegisterAsync(ProfileRegister profile);

        /// <summary>
        /// Unregisters an existing profile.
        /// </summary>
        /// <param name="profile">The profile to be unregistered.</param>
        /// <returns></returns>
        Task UnregisterAsync(ProfileRegister profile);
    }
}
