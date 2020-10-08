using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Observatory.Core.Models;
using Observatory.Core.Persistence;
using Observatory.Core.ViewModels;
using Observatory.Core.ViewModels.Calendar;
using Observatory.Core.ViewModels.Mail;
using Observatory.Providers.Exchange.Persistence;
using Observatory.Providers.Exchange.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using System;
using System.Net.Http.Headers;
using System.Reactive;
using System.Threading.Tasks;

namespace Observatory.Providers.Exchange
{
    public class ExchangeProfileViewModel : ProfileViewModelBase
    {
        private readonly ExchangeProfileDataStore.Factory _storeFactory;
        private readonly GraphServiceClient _client;
        private readonly ExchangeMailService _mailService;

        public override MailBoxViewModel MailBox { get; }

        public override CalendarViewModel Calendar => throw new NotImplementedException();

        public override ReactiveCommand<Unit, Unit> AuthenticateCommand => throw new NotImplementedException();

        public override ReactiveCommand<string, Unit> UpdateNameCommand => throw new NotImplementedException();

        public override ReactiveCommand<Unit, Unit> DeleteCommand => throw new NotImplementedException();

        public ExchangeProfileViewModel(ProfileRegister register,
            ExchangeProfileDataStore.Factory storeFactory,
            ExchangeAuthenticationService authenticationService)
            : base(register)
        {
            _storeFactory = storeFactory;
            _client = new GraphServiceClient(
                "https://graph.microsoft.com/v1.0",
                new DelegateAuthenticationProvider(async (request) =>
                {
                    try
                    {
                        var result = await authenticationService.AcquireTokenSilentAsync(EmailAddress);
                        var token = result.AccessToken;
                        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
                    }
                    catch (Exception ex)
                    {
                        IsAuthenticated = false;
                        this.Log().Error(ex, $"Failed to silently authenticate {EmailAddress}.");
                    }
                }));
            _mailService = new ExchangeMailService(register, _storeFactory, _client);
            MailBox = new MailBoxViewModel(new ExchangeProfileDataQueryFactory(register.DataFilePath, storeFactory), _mailService);
        }

        public async Task RestoreAsync()
        {
            this.Log().Info($"Initializing {_register.EmailAddress} profile.");
            var restoringTasks = Task.WhenAll(
                _mailService.InitializeAsync(),
                MailBox.RestoreAsync());

            var store = _storeFactory.Invoke(_register.DataFilePath);
            var state = await store.Profiles.FirstAsync();
            DisplayName = state.DisplayName;

            await restoringTasks;
        }

        public void Dispose()
        {
            MailBox.Dispose();
        }
    }
}
