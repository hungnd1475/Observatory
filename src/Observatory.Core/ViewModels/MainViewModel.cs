using Autofac.Features.Indexed;
using DynamicData;
using Observatory.Core.Interactivity;
using Observatory.Core.Persistence;
using Observatory.Core.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Observatory.Core.ViewModels
{
    public class MainViewModel : ReactiveObject, IScreen, IActivatableViewModel
    {
        public IObservable<IChangeSet<ProfileViewModelBase>> Profiles { get; }

        public ReactiveCommand<Unit, Unit> AddProfile { get; }

        public ReactiveCommand<FunctionalityMode, Unit> SelectMode { get; }

        public FunctionalityMode[] Modes { get; } = new FunctionalityMode[]
        {
            FunctionalityMode.Mail,
            FunctionalityMode.Calendar,
        };

        [Reactive]
        public FunctionalityMode CurrentMode { get; set; } = FunctionalityMode.Mail;

        public RoutingState Router { get; } = new RoutingState();

        public ViewModelActivator Activator { get; } = new ViewModelActivator();

        public MainViewModel(IProfileRegistrationService profileRegistration,
            ProfilePersistenceConfiguration profilePersistenceConfiguration,
            IEnumerable<IProfileProvider> allProviders,
            IIndex<string, IProfileProvider> indexedProviders,
            IIndex<FunctionalityMode, Func<IFunctionalityViewModel>> functionalityViewModels)
        {
            Profiles = profileRegistration.Connect()
                .ObserveOn(RxApp.TaskpoolScheduler)
                .TransformAsync(p => indexedProviders[p.ProviderId].CreateViewModelAsync(p))
                .Publish()
                .RefCount();

            AddProfile = ReactiveCommand.CreateFromTask(async () =>
            {
                var provider = await Interactions.ProviderSelection.Handle(allProviders);
                if (provider != null)
                {
                    var profile = await provider.CreateRegisterAsync(profilePersistenceConfiguration.ProfileDataDirectory);
                    await profileRegistration.RegisterAsync(profile);
                }
            });
            AddProfile.ThrownExceptions
                .Subscribe(ex => this.Log().Error(ex));

            SelectMode = ReactiveCommand.Create<FunctionalityMode>(mode =>
            {
                CurrentMode = mode;
            });

            this.WhenActivated(disposables =>
            {
                this.WhenAnyValue(x => x.CurrentMode)
                    .DistinctUntilChanged()
                    .Select(mode => functionalityViewModels[mode].Invoke())
                    .InvokeCommand<IRoutableViewModel, IRoutableViewModel>(Router.NavigateAndReset)
                    .DisposeWith(disposables);
            });
        }
    }
}
