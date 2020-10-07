using Autofac.Features.Indexed;
using Observatory.Core.Models;
using Observatory.Core.Persistence;
using Observatory.Core.Services;
using Observatory.Core.ViewModels.Calendar;
using Observatory.Core.ViewModels.Mail;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace Observatory.Core.ViewModels
{
    public class MainViewModel : ReactiveObject, IScreen
    {
        public ReactiveCommand<IProfileProvider, Unit> AddProfile { get; }

        public FunctionalityMode[] Modes { get; } = new FunctionalityMode[]
        {
            FunctionalityMode.Mail,
            FunctionalityMode.Calendar,
        };

        public IEnumerable<IProfileProvider> Providers { get; }

        [Reactive]
        public FunctionalityMode SelectedMode { get; set; } = FunctionalityMode.Mail;

        public RoutingState Router { get; } = new RoutingState();

        public MainViewModel(ProfileRegistrationService profileRegistration,
            ProfilePersistenceConfiguration profilePersistenceConfiguration,
            IEnumerable<IProfileProvider> providers,
            MailManagerViewModel mailViewModel,
            CalendarViewModel calendarViewModel)
        {
            mailViewModel.HostScreen = this;
            calendarViewModel.HostScreen = this;

            Providers = providers;
            AddProfile = ReactiveCommand.CreateFromTask<IProfileProvider, Unit>(async provider =>
            {
                var profile = await provider.CreateRegisterAsync(profilePersistenceConfiguration.ProfileDataDirectory);
                await profileRegistration.RegisterAsync(profile);
                return Unit.Default;
            });
            AddProfile.ThrownExceptions
                .Subscribe(ex => this.Log().Error(ex));

            this.WhenAnyValue(x => x.SelectedMode)
                .DistinctUntilChanged()
                .Do(mode =>
                {
                    switch (mode)
                    {
                        case FunctionalityMode.Mail:
                            Router.Navigate.Execute(mailViewModel);
                            break;
                        case FunctionalityMode.Calendar:
                            Router.Navigate.Execute(calendarViewModel);
                            break;
                    }
                })
                .Subscribe();
        }
    }
}
