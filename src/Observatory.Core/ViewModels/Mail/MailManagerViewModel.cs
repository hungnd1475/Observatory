using Autofac.Features.Indexed;
using DynamicData;
using Observatory.Core.Models.Settings;
using Observatory.Core.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Observatory.Core.ViewModels.Mail
{
    public class MailManagerViewModel : ReactiveObject, IFunctionalityViewModel
    {
        private ReadOnlyObservableCollection<ProfileViewModelBase> _profiles;
        private IDisposable _messageMarkingAsReadWhenViewedSubscription;

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

                this.WhenAnyValue(x => x.SelectedFolder)
                    .DistinctUntilChanged()
                    .Buffer(2, 1)
                    .Select(x => (Previous: x[0], Current: x[1]))
                    .Subscribe(x =>
                    {
                        x.Previous?.ClearMessages();
                        x.Current?.LoadMessages();
                    })
                    .DisposeWith(disposables);

                this.WhenAnyValue(x => x.SelectedMessage)
                    .Buffer(2, 1)
                    .Select(x => (Previous: x[0], Current: x[1]))
                    .Subscribe(x =>
                    {
                        _messageMarkingAsReadWhenViewedSubscription?.Dispose();
                        switch (settings.MarkingAsReadBehavior)
                        {
                            case MarkingAsReadBehavior.WhenViewed:
                                if (x.Current != null && !x.Current.IsRead)
                                {
                                    _messageMarkingAsReadWhenViewedSubscription = Observable
                                        .Timer(TimeSpan.FromSeconds(settings.MarkingAsReadWhenViewedSeconds))
                                        .ObserveOn(RxApp.MainThreadScheduler)
                                        .Subscribe(_ =>
                                        {
                                            x.Current.ToggleRead
                                                .Execute()
                                                .Subscribe();
                                        });
                                }
                                break;
                            case MarkingAsReadBehavior.WhenSelectionChanged:
                                if (x.Previous != null && !x.Previous.IsRead)
                                {
                                    x.Previous.ToggleRead
                                        .Execute()
                                        .Subscribe();
                                }
                                break;
                        }
                    })
                    .DisposeWith(disposables);

                Disposable.Create(() =>
                {
                    _profiles = null;
                    _messageMarkingAsReadWhenViewedSubscription?.Dispose();
                    _messageMarkingAsReadWhenViewedSubscription = null;

                    SelectedFolder?.ClearMessages();

                    SelectedProfile = null;
                    SelectedFolder = null;
                    SelectedMessage = null;
                })
                .DisposeWith(disposables);
            });
        }
    }
}
