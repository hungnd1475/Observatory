using DynamicData;
using Observatory.Core.Models;
using Observatory.Core.Persistence;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Core.ViewModels.Mail
{
    public class MailFolderViewModel : ReactiveObject, IDisposable
    {
        private readonly string _folderId;
        private readonly IProfileDataQueryFactory _queryFactory;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly ReadOnlyObservableCollection<MailFolderViewModel> _childFolders;
        private readonly SourceCache<MessageSummary, string> _sourceMessages =
            new SourceCache<MessageSummary, string>(m => m.Id);
        private readonly ReadOnlyObservableCollection<MessageSummaryViewModel> _messages;

        [Reactive]
        public string Name { get; private set; }

        [ObservableAsProperty]
        public int UnreadCount { get; }

        [ObservableAsProperty]
        public int TotalCount { get; }

        public FolderType Type { get; }

        [Reactive]
        public bool IsFavorite { get; set; }

        public ReadOnlyObservableCollection<MailFolderViewModel> ChildFolders => _childFolders;

        public ReadOnlyObservableCollection<MessageSummaryViewModel> Messages => _messages;

        public ReactiveCommand<Unit, Unit> SynchronizeCommand { get; }

        public ReactiveCommand<Unit, Unit> CancelSynchronizationCommand { get; }

        public bool IsSynchronizing { get; }

        public ReactiveCommand<Unit, Unit> RenameCommand => throw new NotImplementedException();

        public ReactiveCommand<string, Unit> MoveCommand => throw new NotImplementedException();

        public ReactiveCommand<Unit, Unit> DeleteCommand => throw new NotImplementedException();

        public MailFolderViewModel(Node<MailFolder, string> node, IProfileDataQueryFactory queryFactory)
        {
            _folderId = node.Item.Id;
            _queryFactory = queryFactory;

            node.Children.Connect()
                .Transform(n => new MailFolderViewModel(n, queryFactory))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _childFolders)
                .DisposeMany()
                .Subscribe()
                .DisposeWith(_disposables);

            _sourceMessages.Connect()
                .Transform(m => new MessageSummaryViewModel(m))
                .Bind(out _messages)
                .Subscribe()
                .DisposeWith(_disposables);

            Name = node.Item.Name;
            Type = node.Item.Type;
            IsFavorite = node.Item.IsFavorite;

            SynchronizeCommand = ReactiveCommand.CreateFromTask(RestoreAsync);
        }

        public async Task RestoreAsync()
        {
            using var query = _queryFactory.Connect();
            var messages = await query.GetMessageSummariesAsync(messages => messages.Where(m => m.FolderId == _folderId));
            _sourceMessages.AddOrUpdate(messages);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
