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
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Observatory.Application.ViewModels.Mail
{
    public class MailBoxViewModel : ReactiveObject
    {
        private readonly IProfileDataQueryFactory _queryFactory;
        private readonly IMailService _mailService;
        private readonly SourceCache<MailFolder, string> _sourceFolders =
            new SourceCache<MailFolder, string>(f => f.Id);
        private readonly ReadOnlyObservableCollection<MailFolderViewModel> _displayFolders;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public ReadOnlyObservableCollection<MailFolderViewModel> Folders => _displayFolders;
        public ReactiveCommand<Unit, Unit> SynchronizeCommand { get; }

        [ObservableAsProperty]
        public bool IsSynchronizing { get; }

        public MailBoxViewModel(IProfileDataQueryFactory profileQueryFactory, IMailService mailService)
        {
            _queryFactory = profileQueryFactory;
            _mailService = mailService;

            _sourceFolders.Connect()
                .And(new[] { _mailService.FolderChanges })
                .Sort(SortExpressionComparer<MailFolder>.Ascending(f => f.Type).ThenByAscending(f => f.Name))
                .TransformToTree(f => f.ParentId)
                .Transform(n => new MailFolderViewModel(n))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _displayFolders)
                .DisposeMany()
                .Subscribe()
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
        }
    }
}
