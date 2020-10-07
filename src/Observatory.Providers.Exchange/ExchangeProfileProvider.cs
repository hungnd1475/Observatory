using Microsoft.EntityFrameworkCore;
using Observatory.Core.Models;
using Observatory.Core.Services;
using Observatory.Core.ViewModels;
using Observatory.Providers.Exchange.Models;
using Observatory.Providers.Exchange.Persistence;
using Observatory.Providers.Exchange.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Observatory.Providers.Exchange
{
    public class ExchangeProfileProvider : IProfileProvider
    {
        public const string PROVIDER_ID = "Exchange";
        private readonly ExchangeAuthenticationService _authenticationService;

        public ExchangeProfileProvider(ExchangeAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        public string DisplayName { get; } = "Exchange";

        public string IconGeometry { get; } = "M3,18L7,16.75V7L14,5V19.5L3.5,18.25L14,22L20,20.75V3.5L13.95,2L3,5.75V18Z";

        public async Task<ProfileRegister> CreateRegisterAsync(string profileDataDirectory)
        {
            var result = await _authenticationService.AcquireTokenInteractiveAsync();
            var emailAddress = result.Account.Username;
            var profileDataPath = Path.Combine(profileDataDirectory, emailAddress);
            var store = new ExchangeProfileDataStore(profileDataPath);
            await store.Database.EnsureCreatedAsync();

            store.Profiles.Add(new Profile()
            {
                EmailAddress = emailAddress,
                DisplayName = emailAddress,
                ProviderId = PROVIDER_ID,
            });
            store.FolderSynchronizationStates.Add(new FolderSynchronizationState());
            store.MessageSynchronizationStates.Add(new MessageSynchronizationState());
            await store.SaveChangesAsync();

            return new ProfileRegister() 
            { 
                Id = emailAddress,
                EmailAddress = emailAddress,
                DataFilePath = profileDataPath,
                ProviderId = PROVIDER_ID,
            };
        }

        public async Task<ProfileViewModelBase> CreateViewModelAsync(ProfileRegister register)
        {
            var storeFactory = new ExchangeProfileDataStoreFactory(register.DataFilePath);
            var profile = new ExchangeProfileViewModel(register.EmailAddress, storeFactory, _authenticationService);
            await profile.RestoreAsync();
            return profile;
        }
    }
}
