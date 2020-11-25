using DynamicData;
using DynamicData.Binding;
using Observatory.Core.Models;
using Observatory.Core.Persistence;
using Observatory.Core.Persistence.Specifications;
using Observatory.Core.Services;
using Observatory.Core.Virtualization;
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
    public class MailFolderViewModel : ReactiveObject, IDisposable
    {
        private readonly string _folderId;
        private readonly IProfileDataQueryFactory _queryFactory;
        private readonly IMailService _mailService;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly ReadOnlyObservableCollection<MailFolderViewModel> _childFolders;

        [Reactive]
        public string Name { get; private set; }

        [Reactive]
        public int UnreadCount { get; private set; }

        [Reactive]
        public int TotalCount { get; private set; }

        public FolderType Type { get; }

        [Reactive]
        public bool IsFavorite { get; private set; }

        public ReadOnlyObservableCollection<MailFolderViewModel> ChildFolders => _childFolders;

        [Reactive]
        public VirtualizingCache<MessageSummary, string> Messages { get; private set; }

        public ReactiveCommand<Unit, Unit> Synchronize { get; }

        public ReactiveCommand<Unit, Unit> CancelSynchronization { get; }

        [ObservableAsProperty]
        public bool IsSynchronizing { get; }

        public ReactiveCommand<Unit, Unit> Rename => throw new NotImplementedException();

        public ReactiveCommand<string, Unit> Move => throw new NotImplementedException();

        public ReactiveCommand<Unit, Unit> Delete => throw new NotImplementedException();

        public MailFolderViewModel(Node<MailFolder, string> node,
            IProfileDataQueryFactory queryFactory,
            IMailService mailService)
        {
            _folderId = node.Item.Id;
            _queryFactory = queryFactory;
            _mailService = mailService;

            node.Children.Connect()
                .Transform(n => new MailFolderViewModel(n, queryFactory, _mailService))
                .Sort(SortExpressionComparer<MailFolderViewModel>.Ascending(f => f.Name))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _childFolders)
                .DisposeMany()
                .Subscribe()
                .DisposeWith(_disposables);

            _mailService.MessageChanges
                .Where(d => d.FolderId == _folderId)
                .SelectMany(_ => CountMessages())
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    UnreadCount = x.UnreadCount;
                    TotalCount = x.TotalCount;
                })
                .DisposeWith(_disposables);

            CountMessages()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    UnreadCount = x.UnreadCount;
                    TotalCount = x.TotalCount;
                })
                .DisposeWith(_disposables);

            Synchronize = ReactiveCommand.CreateFromObservable(() => Observable
                .StartAsync((token) => _mailService.SynchronizeMessagesAsync(_folderId, token))
                .TakeUntil(CancelSynchronization));
            Synchronize.IsExecuting
                .ToPropertyEx(this, x => x.IsSynchronizing)
                .DisposeWith(_disposables);
            Synchronize.ThrownExceptions
                .Subscribe(ex => this.Log().Error(ex))
                .DisposeWith(_disposables);
            CancelSynchronization = ReactiveCommand.Create(() => { }, Synchronize.IsExecuting);

            Name = node.Item.Name;
            Type = node.Item.Type;
            IsFavorite = node.Item.IsFavorite;
        }

        public MessageSummaryViewModel Transform(MessageSummary state)
        {
            return new MessageSummaryViewModel(state, _queryFactory);
        }

        private IObservable<(int UnreadCount, int TotalCount)> CountMessages()
        {
            return Observable.Start(() =>
            {
                using var query = _queryFactory.Connect();
                var unreadCount = query.MessageSummaries.Count(m => m.FolderId == _folderId && !m.IsRead);
                var totalCount = query.MessageSummaries.Count(m => m.FolderId == _folderId);
                return (UnreadCount: unreadCount, TotalCount: totalCount);
            }, RxApp.TaskpoolScheduler);
        }

        public void InitializeMessages()
        {
            Messages = new VirtualizingCache<MessageSummary, string>(
                new MessageVirtualizingSource(_queryFactory, _folderId),
                m => m.Id,
                _mailService.MessageChanges
                    .Where(d => d.FolderId == _folderId)
                    .Select(d => d.Changes.Select(e => new DeltaEntity<MessageSummary>(e.State, e.Entity.Summary())).ToArray()));
        }

        public void ClearMessages()
        {
            Messages?.Dispose();
            Messages = null;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }

    public class MessageSummaryEqualityComparer : IEqualityComparer<MessageSummary>
    {
        public bool Equals(MessageSummary x, MessageSummary y) => x.Id == y.Id;
        public int GetHashCode(MessageSummary obj) => obj.Id.GetHashCode();
    }
}
