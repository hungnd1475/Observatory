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
    /// Represents a service to handle profiles registration.
    /// </summary>
    public class PersistentProfileRegistrationService :IProfileRegistrationService
    {
        private readonly string _storePath;
        private readonly SourceList<ProfileRegister> _sourceProfiles =
            new SourceList<ProfileRegister>();

        public IObservable<IChangeSet<ProfileRegister>> Connect() => _sourceProfiles.Connect();

        /// <summary>
        /// Constructs a new instance of <see cref="PersistentProfileRegistrationService"/>.
        /// </summary>
        /// <param name="path">The path to the registry file.</param>
        public PersistentProfileRegistrationService(string path)
        {
            _storePath = path;             
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            using var store = new ProfileRegistry(_storePath);
            var profiles = await store.Database.EnsureCreatedAsync(cancellationToken) 
                ? new ProfileRegister[0] 
                : await store.Profiles.ToArrayAsync(cancellationToken);
            _sourceProfiles.AddRange(profiles);
        }

        public async Task RegisterAsync(ProfileRegister profile)
        {
            using var store = new ProfileRegistry(_storePath);
            store.Profiles.Add(profile);
            await store.SaveChangesAsync();
            _sourceProfiles.Add(profile);
        }

        public async Task UnregisterAsync(ProfileRegister profile)
        {
            using var store = new ProfileRegistry(_storePath);
            store.Profiles.Remove(profile);
            await store.SaveChangesAsync();
            _sourceProfiles.Remove(profile);
        }
    }
}
