using DynamicData;
using DynamicData.Binding;
using Observatory.Core.Interactivity;
using Observatory.Core.Models;
using Observatory.Core.Persistence;
using Observatory.Core.Persistence.Specifications;
using Observatory.Core.Services;
using Observatory.Core.Services.ChangeTracking;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Observatory.Core.ViewModels.Mail
{
    public class MailBoxViewModel : ReactiveObject, IDisposable
    {
        private readonly IProfileDataQueryFactory _queryFactory;
        private readonly IMailService _mailService;
        private readonly SourceCache<MailFolder, string> _sourceFolders =
            new SourceCache<MailFolder, string>(f => f.Id);
        private readonly ReadOnlyObservableCollection<MailFolderViewModel> _allFolders;
        private readonly ReadOnlyObservableCollection<MailFolderViewModel> _favoriteFolders;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly MailFolderSelectionViewModel _mailFolderSelectionViewModel;

        public ReadOnlyObservableCollection<MailFolderViewModel> AllFolders => _allFolders;
        public ReadOnlyObservableCollection<MailFolderViewModel> FavoriteFolders => _favoriteFolders;
        public ReactiveCommand<Unit, Unit> Synchronize { get; }

        [ObservableAsProperty]
        public bool IsSynchronizing { get; }

        [ObservableAsProperty]
        public MailFolderViewModel Inbox { get; }

        public MailBoxViewModel(IProfileDataQueryFactory queryFactory, IMailService mailService)
        {
            _queryFactory = queryFactory;
            _mailService = mailService;
            _mailFolderSelectionViewModel = new MailFolderSelectionViewModel(_sourceFolders.AsObservableCache());

            var folderChanges = _sourceFolders.Connect(f => f.Type != FolderType.Root)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Sort(SortExpressionComparer<MailFolder>.Ascending(f => f.Type).ThenByAscending(f => f.Name))
                .TransformToTree(f => f.ParentId)
                .Transform(n => new MailFolderViewModel(n, this, queryFactory, mailService))
                .DisposeMany()
                .Publish();
            folderChanges
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _allFolders)
                .Subscribe()
                .DisposeWith(_disposables);
            folderChanges
                .Filter(f => f.IsFavorite)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _favoriteFolders)
                .Subscribe()
                .DisposeWith(_disposables);
            folderChanges.Connect()
                .DisposeWith(_disposables);

            var folderCollection = folderChanges
                .ToCollection()
                .Publish();
            folderCollection
                .Select(folders => folders.FirstOrDefault(f => f.Type == FolderType.Inbox))
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToPropertyEx(this, x => x.Inbox)
                .DisposeWith(_disposables);
            folderCollection.Connect()
                .DisposeWith(_disposables);

            _mailService.FolderChanges
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Subscribe(changes =>
                {
                    _sourceFolders.Edit(updater =>
                    {
                        foreach (var c in changes)
                        {
                            switch (c.State)
                            {
                                case DeltaState.Add:
                                case DeltaState.Update:
                                    updater.AddOrUpdate(c.Entity);
                                    break;
                                case DeltaState.Remove:
                                    updater.RemoveKey(c.Entity.Id);
                                    break;
                            }
                        }
                    });
                })
                .DisposeWith(_disposables);

            Synchronize = ReactiveCommand.CreateFromTask(_mailService.SynchronizeFoldersAsync);
            //Synchronize.WithLatestFrom(folderCollection, (_, folders) => folders.Where(f => f.IsFavorite))
            //    .Subscribe(favoriteFolders =>
            //    {
            //        foreach (var f in favoriteFolders)
            //        {
            //            f.Synchronize.Execute().Subscribe();
            //        }
            //    })
            //    .DisposeWith(_disposables);
            Synchronize.ThrownExceptions
                .Subscribe(ex => this.Log().Error(ex))
                .DisposeWith(_disposables);
            Synchronize.IsExecuting
                .ToPropertyEx(this, x => x.IsSynchronizing)
                .DisposeWith(_disposables);
        }

        public async Task<MailFolderSelectionResult> PromptUserToSelectFolder(string titleText,
            string promptText, bool includeRoot, Func<MailFolderSelectionItem, bool> canMoveTo)
        {
            _mailFolderSelectionViewModel.TitleText = titleText;
            _mailFolderSelectionViewModel.PromptText = promptText;
            _mailFolderSelectionViewModel.IncludeRoot = includeRoot;
            _mailFolderSelectionViewModel.CanMoveTo = canMoveTo;
            return await Interactions.MailFolderSelection.Handle(_mailFolderSelectionViewModel);
        }

        public void Restore()
        {
            Observable.Start(() =>
            {
                using var query = _queryFactory.Connect();
                var folders = query.Folders.ToList();
                _sourceFolders.Edit(updater => updater.Load(folders));
            },
            RxApp.TaskpoolScheduler);

            Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(30))
                .Select(_ => Unit.Default)
                .InvokeCommand(Synchronize)
                .DisposeWith(_disposables);
        }

        public void Dispose()
        {
            _mailFolderSelectionViewModel.Dispose();
            _disposables.Dispose();
        }
    }
}
