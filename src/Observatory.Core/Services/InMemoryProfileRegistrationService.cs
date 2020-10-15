using DynamicData;
using Observatory.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Observatory.Core.Services
{
    public class InMemoryProfileRegistrationService : IProfileRegistrationService
    {
        private readonly SourceList<ProfileRegister> _sourceProfiles =
            new SourceList<ProfileRegister>();

        public IObservable<IChangeSet<ProfileRegister>> Connect() => _sourceProfiles.Connect();

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RegisterAsync(ProfileRegister profile)
        {
            _sourceProfiles.Add(profile);
            return Task.CompletedTask;
        }

        public Task UnregisterAsync(ProfileRegister profile)
        {
            _sourceProfiles.Remove(profile);
            return Task.CompletedTask;
        }
    }
}
