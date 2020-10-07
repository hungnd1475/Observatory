using DynamicData;
using DynamicData.Binding;
using Microsoft.EntityFrameworkCore;
using Observatory.Core.Models;
using Observatory.Core.Persistence;
using Observatory.Core.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
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

        public ReadOnlyObservableCollection<MailFolderViewModel> AllFolders => _allFolders;
        public ReadOnlyObservableCollection<MailFolderViewModel> FavoriteFolders => _favoriteFolders;
        public ReactiveCommand<Unit, Unit> SynchronizeCommand { get; }

        [ObservableAsProperty]
        public bool IsSynchronizing { get; }

        [ObservableAsProperty]
        public MailFolderViewModel Inbox { get; }

        public MailBoxViewModel(IProfileDataQueryFactory profileQueryFactory, IMailService mailService)
        {
            _queryFactory = profileQueryFactory;
            _mailService = mailService;

            var sharedFoldersConnection = _sourceFolders.Connect()
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Or(new[] { _mailService.FolderChanges })
                .Sort(SortExpressionComparer<MailFolder>.Ascending(f => f.Type).ThenByAscending(f => f.Name))
                .TransformToTree(f => f.ParentId)
                .Transform(n => new MailFolderViewModel(n))
                .Publish();
            sharedFoldersConnection
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _allFolders)
                .Subscribe()
                .DisposeWith(_disposables);
            sharedFoldersConnection
                .Filter(f => f.IsFavorite)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _favoriteFolders)
                .Subscribe()
                .DisposeWith(_disposables);
            sharedFoldersConnection
                .ToCollection()
                .Select(folders => folders.FirstOrDefault(f => f.Type == FolderType.Inbox))
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToPropertyEx(this, x => x.Inbox)
                .DisposeWith(_disposables);
            sharedFoldersConnection.Connect()
                .DisposeWith(_disposables);

            SynchronizeCommand = ReactiveCommand.CreateFromTask(_mailService.SynchronizeFoldersAsync);
            SynchronizeCommand.IsExecuting
                .ToPropertyEx(this, x => x.IsSynchronizing)
                .DisposeWith(_disposables);
        }

        public async Task RestoreAsync()
        {
            using var query = _queryFactory.Connect();
            var folders = await query.Folders.ToListAsync();
            _sourceFolders.Edit(updater => updater.Load(folders));
            SynchronizeCommand.Execute().Subscribe();
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
