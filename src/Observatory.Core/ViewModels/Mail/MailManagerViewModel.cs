using Autofac.Features.Indexed;
using DynamicData;
using Microsoft.EntityFrameworkCore.Internal;
using Observatory.Core.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Observatory.Core.ViewModels.Mail
{
    public class MailManagerViewModel : ReactiveObject, IRoutableViewModel, IDisposable
    {
        private readonly ReadOnlyObservableCollection<ProfileViewModelBase> _profiles;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public MailManagerViewModel(ProfileRegistrationService profileRegistrationService,
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

            this.WhenAnyValue(x => x.SelectedProfile)
                .DistinctUntilChanged()
                .Buffer(2, 1)
                .Select(x => (Previous: x[0], Current: x[1]))
                .Do(x =>
                {
                    if (x.Previous != null) x.Previous.IsSelected = false;
                    if (x.Current != null) x.Current.IsSelected = true;
                })
                .Subscribe()
                .DisposeWith(_disposables);

            this.WhenAnyValue(x => x.SelectedFolder)
                .DistinctUntilChanged()
                .Buffer(2, 1)
                .Select(x => (Previous: x[0], Current: x[1]))
                .Do(x =>
                {
                    if (x.Previous != null) x.Previous.IsSelected = false;
                    if (x.Current != null) x.Current.IsSelected = true;
                })
                .Subscribe()
                .DisposeWith(_disposables);
        }

        public ReadOnlyObservableCollection<ProfileViewModelBase> Profiles => _profiles;

        [Reactive]
        public ProfileViewModelBase SelectedProfile { get; set; }

        [Reactive]
        public MailFolderViewModel SelectedFolder { get; set; }

        public string UrlPathSegment { get; } = "mail";

        public IScreen HostScreen { get; set; }

        public MainViewModel Main => (MainViewModel)HostScreen;

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
