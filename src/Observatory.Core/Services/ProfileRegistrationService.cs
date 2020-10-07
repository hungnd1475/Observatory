using DynamicData;
using DynamicData.Alias;
using Microsoft.EntityFrameworkCore;
using Observatory.Core.Models;
using Observatory.Core.Persistence;
using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Observatory.Core.Services
{
    /// <summary>
    /// Represents a service for registering profiles.
    /// </summary>
    public class ProfileRegistrationService
    {
        private readonly string _storePath;
        private readonly SourceList<ProfileRegister> _sourceProfiles =
            new SourceList<ProfileRegister>();

        /// <summary>
        /// Connects to the changes in profile registers.
        /// </summary>
        /// <returns></returns>
        public IObservable<IChangeSet<ProfileRegister>> Connect() => _sourceProfiles.Connect();

        /// <summary>
        /// Constructs a new instance of <see cref="ProfileRegistrationService"/>.
        /// </summary>
        /// <param name="path">The path to the registry file.</param>
        public ProfileRegistrationService(string path)
        {
            _storePath = path;             
        }

        /// <summary>
        /// Initializes the service, creating the registry file or loading existing profiles.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            using var store = new ProfileRegistry(_storePath);
            var profiles = await store.Database.EnsureCreatedAsync(cancellationToken) 
                ? new ProfileRegister[0] 
                : await store.Profiles.ToArrayAsync(cancellationToken);
            _sourceProfiles.AddRange(profiles);
        }

        /// <summary>
        /// Registers a new profile.
        /// </summary>
        /// <param name="profile">The profile to be registered.</param>
        /// <returns></returns>
        public async Task RegisterAsync(ProfileRegister profile)
        {
            using var store = new ProfileRegistry(_storePath);
            store.Profiles.Add(profile);
            await store.SaveChangesAsync();
            _sourceProfiles.Add(profile);
        }

        /// <summary>
        /// Unregisters an existing profile.
        /// </summary>
        /// <param name="profile">The profile to be unregistered.</param>
        /// <returns></returns>
        public async Task UnregisterAsync(ProfileRegister profile)
        {
            using var store = new ProfileRegistry(_storePath);
            store.Profiles.Remove(profile);
            await store.SaveChangesAsync();
            _sourceProfiles.Remove(profile);
        }
    }
}
