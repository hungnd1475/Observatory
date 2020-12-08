using Observatory.Core.Models;
using Observatory.Core.Services;
using Observatory.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Core.Providers.Fake
{
    public class FakeProfileProvider : IProfileProvider
    {
        private readonly ProfileRegister _register;
        private readonly FakeProfileViewModel _viewModel;

        public string DisplayName { get; } = "Fake Provider 01";

        public string IconGeometry { get; } = "F1 M 10 0 L 20 10 L 10 20 L 0 10 Z M 1.328125 10 L 10 18.671875 L 18.671875 10 L 10 1.328125 Z";

        public FakeProfileProvider(string providerId, string emailAddress, string displayName,
            FakeProfileDataQueryFactory queryFactory, FakeMailService mailService)
        {
            _register = new ProfileRegister()
            {
                Id = emailAddress,
                EmailAddress = emailAddress,
                DataFilePath = null,
                ProviderId = providerId,
            };
            _viewModel = new FakeProfileViewModel(_register, new Profile()
            {
                DisplayName = displayName,
                EmailAddress = emailAddress,
                ProviderId = providerId,
            }, queryFactory, mailService);
            _viewModel.Restore();
        }

        public Task<ProfileRegister> CreateRegisterAsync(string profileDataDirectory)
        {
            return Task.FromResult(_register);
        }

        public Task<ProfileViewModelBase> CreateViewModelAsync(ProfileRegister register)
        {
            return Task.FromResult<ProfileViewModelBase>(_viewModel);
        }

        public Stream ReadIcon()
        {
            throw new NotImplementedException();
        }
    }
}
