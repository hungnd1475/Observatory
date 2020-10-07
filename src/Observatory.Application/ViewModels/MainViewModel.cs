using DynamicData;
using Observatory.Core.Models;
using Observatory.Core.Services;
using Observatory.Core.ViewModels;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace Observatory.Application.ViewModels
{
    public class MainViewModel : ReactiveObject
    {
        private readonly ReadOnlyObservableCollection<IProfileViewModel> _profiles;

        public ReadOnlyObservableCollection<IProfileViewModel> Profiles => _profiles;
        public Interaction<ProfileRegistrationViewModel, ProfileRegister> ProfileRegistration { get; } =
            new Interaction<ProfileRegistrationViewModel, ProfileRegister>();
        public ReactiveCommand<Unit, Unit> AddProfile { get; }

        public MainViewModel(ProfileRegistrationService profileRegistration,
            ProfileRegistrationViewModel profileRegistrationViewModel,
            Func<string, IProfileProvider> providers)
        {
            profileRegistration.Connect()
                .TransformAsync(p => providers(p.ProviderId).CreateViewModelAsync(p))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _profiles)
                .Subscribe();

            AddProfile = ReactiveCommand.CreateFromObservable(() =>
            {
                return ProfileRegistration.Handle(profileRegistrationViewModel)
                    .SelectMany(async profile =>
                    {
                        await profileRegistration.RegisterAsync(profile);
                        return Unit.Default;
                    });
            });
        }
    }
}
