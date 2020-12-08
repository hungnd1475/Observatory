using Microsoft.EntityFrameworkCore;
using Observatory.Core.Models;
using Observatory.Core.Services;
using Observatory.Core.ViewModels;
using Observatory.Providers.Exchange.Models;
using Observatory.Providers.Exchange.Persistence;
using Observatory.Providers.Exchange.Services;
using Splat;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Observatory.Providers.Exchange
{
    public class ExchangeProfileProvider : IProfileProvider, IEnableLogger
    {
        public const string PROVIDER_ID = "Exchange";
        private readonly ExchangeAuthenticationService _authenticationService;
        private readonly ExchangeProfileDataStore.Factory _storeFactory;

        public ExchangeProfileProvider(ExchangeAuthenticationService authenticationService,
            ExchangeProfileDataStore.Factory storeFactory)
        {
            _authenticationService = authenticationService;
            _storeFactory = storeFactory;
        }

        public string DisplayName { get; } = "Microsoft Exchange";

        public string IconGeometry { get; } = "M3,18L7,16.75V7L14,5V19.5L3.5,18.25L14,22L20,20.75V3.5L13.95,2L3,5.75V18Z";

        public async Task<ProfileRegister> CreateRegisterAsync(string profileDataDirectory)
        {
            var result = await _authenticationService.AcquireTokenInteractiveAsync();
            var emailAddress = result.Account.Username;
            var profileDataPath = Path.Combine(profileDataDirectory, emailAddress);
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
            var profile = new ExchangeProfileViewModel(register, _storeFactory, _authenticationService);
            await profile.RestoreAsync();
            return profile;
        }

        public Stream ReadIcon()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("Observatory.Providers.Exchange.logo.png");
        }
    }
}
