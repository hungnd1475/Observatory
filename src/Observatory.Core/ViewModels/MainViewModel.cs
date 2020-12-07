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
        public Interaction<IEnumerable<IProfileProvider>, IProfileProvider> ProviderSelection { get; } =
            new Interaction<IEnumerable<IProfileProvider>, IProfileProvider>();

        public ReactiveCommand<Unit, Unit> AddProfile { get; }

        public FunctionalityMode[] Modes { get; } = new FunctionalityMode[]
        {
            FunctionalityMode.Mail,
            FunctionalityMode.Calendar,
        };

        [Reactive]
        public FunctionalityMode SelectedMode { get; set; } = FunctionalityMode.Mail;

        public RoutingState Router { get; } = new RoutingState();

        public MainViewModel(IProfileRegistrationService profileRegistration,
            ProfilePersistenceConfiguration profilePersistenceConfiguration,
            IEnumerable<IProfileProvider> providers,
            MailManagerViewModel mailViewModel,
            CalendarViewModel calendarViewModel)
        {
            mailViewModel.HostScreen = this;
            calendarViewModel.HostScreen = this;

            AddProfile = ReactiveCommand.CreateFromTask(async () =>
            {
                var provider = await ProviderSelection.Handle(providers);
                if (provider != null)
                {
                    var profile = await provider.CreateRegisterAsync(profilePersistenceConfiguration.ProfileDataDirectory);
                    await profileRegistration.RegisterAsync(profile);
                }
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
