using Autofac.Features.Indexed;
using DynamicData;
using Observatory.Core.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Observatory.Core.ViewModels.Mail
{
    public class MailManagerViewModel : ReactiveObject, IRoutableViewModel, IDisposable
    {
        private readonly ReadOnlyObservableCollection<ProfileViewModelBase> _profiles;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public ReadOnlyObservableCollection<ProfileViewModelBase> Profiles => _profiles;

        [Reactive]
        public ProfileViewModelBase SelectedProfile { get; set; }

        [Reactive]
        public MailFolderViewModel SelectedFolder { get; set; }

        [Reactive]
        public MessageSummaryViewModel SelectedMessage { get; set; }

        public string UrlPathSegment { get; } = "mail";

        IScreen IRoutableViewModel.HostScreen => HostScreen;

        public MainViewModel HostScreen { get; set; }

        public MailManagerViewModel(IProfileRegistrationService profileRegistrationService,
            IIndex<string, IProfileProvider> providers)
        {
            var sharedProfilesConnection = profileRegistrationService.Connect()
                .ObserveOn(RxApp.TaskpoolScheduler)
                .TransformAsync(p => providers[p.ProviderId].CreateViewModelAsync(p))
                .Publish();
            sharedProfilesConnection
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _profiles)
                .DisposeMany()
                .Subscribe(_ => { }, ex => this.Log().Error(ex))
                .DisposeWith(_disposables);
            sharedProfilesConnection
                .Where(_ => SelectedProfile == null)
                .ToCollection()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Do(profiles => SelectedProfile = profiles.FirstOrDefault())
                .Subscribe()
                .DisposeWith(_disposables);
            sharedProfilesConnection
                .Connect()
                .DisposeWith(_disposables);

            this.WhenAnyValue(x => x.SelectedProfile)
                .Where(p => p != null)
                .DistinctUntilChanged()
                .SelectMany(p => p.WhenAnyValue(x => x.MailBox.Inbox))
                .Do(f => SelectedFolder = f)
                .Subscribe()
                .DisposeWith(_disposables);

            this.WhenAnyValue(x => x.SelectedMessage)
                .Subscribe(m => this.Log().Debug($"Selected message: {m?.Subject}"));
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
