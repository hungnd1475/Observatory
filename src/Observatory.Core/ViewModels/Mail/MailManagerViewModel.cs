using DynamicData;
using Observatory.Core.Models.Settings;
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
    public class MailManagerViewModel : ReactiveObject, IFunctionalityViewModel
    {
        private ReadOnlyObservableCollection<ProfileViewModelBase> _profiles;

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

        public ViewModelActivator Activator { get; } = new ViewModelActivator();

        public MailManagerViewModel(MailSettings settings)
        {
            this.WhenActivated(disposables =>
            {
                HostScreen.Profiles
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Bind(out _profiles)
                    .DisposeMany()
                    .Subscribe(_ => { }, ex => this.Log().Error(ex))
                    .DisposeWith(disposables);

                HostScreen.Profiles
                    .Where(_ => SelectedProfile == null)
                    .ToCollection()
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(profiles => SelectedProfile = profiles.FirstOrDefault())
                    .DisposeWith(disposables);

                this.RaisePropertyChanged(nameof(Profiles));

                this.WhenAnyValue(x => x.SelectedProfile)
                    .Where(p => p != null)
                    .DistinctUntilChanged()
                    .SelectMany(p => p.WhenAnyValue(x => x.MailBox.Inbox))
                    .Subscribe(f => SelectedFolder = f)
                    .DisposeWith(disposables);

                this.WhenAnyValue(x => x.SelectedMessage)
                    .DistinctUntilChanged()
                    .Buffer(2, 1)
                    .Select(x => (Previous: x[0], Current: x[1]))
                    .Do(x =>
                    {
                        switch (settings.MarkingAsReadBehavior)
                        {
                            case MarkingAsReadBehavior.WhenViewed:
                                x.Previous?.StopMarkingAsRead();
                                x.Current?.StartMarkingAsRead(settings.MarkingAsReadWhenViewedSeconds);
                                break;
                            case MarkingAsReadBehavior.WhenSelectionChanged:
                                x.Previous?.StartMarkingAsRead();
                                break;
                        }
                    })
                    .Subscribe()
                    .DisposeWith(disposables);

                Disposable.Create(() =>
                {
                    _profiles = null;
                    SelectedProfile = null;
                    SelectedFolder = null;
                    SelectedMessage = null;
                })
                .DisposeWith(disposables);
            });
        }
    }
}
