using DynamicData;
using Observatory.Core.Models;
using Observatory.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Observatory.Core.Providers.Fake
{
    public class FakeProfileRegistrationService : IProfileRegistrationService
    {
        private readonly SourceList<ProfileRegister> _sourceProfiles =
            new SourceList<ProfileRegister>();

        public IObservable<IChangeSet<ProfileRegister>> Connect() => _sourceProfiles.Connect();

        public FakeProfileRegistrationService(IEnumerable<ProfileRegister> profiles)
        {
            _sourceProfiles.AddRange(profiles);
        }

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RegisterAsync(ProfileRegister profile)
        {
            return Task.CompletedTask;
        }

        public Task UnregisterAsync(ProfileRegister profile)
        {
            return Task.CompletedTask;
        }
    }
}
