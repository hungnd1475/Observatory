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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Core.ViewModels.Mail
{
    public class MailFolderViewModel : ReactiveObject, IDisposable
    {
        private readonly string _folderId;
        private readonly IProfileDataQueryFactory _queryFactory;
        private readonly IMailService _mailService;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly ReadOnlyObservableCollection<MailFolderViewModel> _childFolders;

        //private readonly SourceCache<MessageSummary, string> _sourceMessages =
        //    new SourceCache<MessageSummary, string>(m => m.Id);
        //private readonly ReadOnlyObservableCollection<MessageSummaryViewModel> _messages;

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

        //public ReadOnlyObservableCollection<MessageSummaryViewModel> Messages
        //{
        //    get
        //    {
        //        LoadMessages();
        //        return _messages;
        //    }
        //}

        [Reactive]
        public VirtualizingCache<MessageSummary, MessageSummaryViewModel> Messages { get; private set; }

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

            //_sourceMessages.Connect(m => m.FolderId == _folderId)
            //    .ObserveOn(RxApp.TaskpoolScheduler)
            //    .Transform(m => new MessageSummaryViewModel(m, _queryFactory))
            //    .Sort(SortExpressionComparer<MessageSummaryViewModel>.Descending(m => m.ReceivedDateTime))
            //    .ObserveOn(RxApp.MainThreadScheduler)
            //    .Bind(out _messages)
            //    .DisposeMany()
            //    .Subscribe()
            //    .DisposeWith(_disposables);

            //_mailService.MessageChanges
            //    .ObserveOn(RxApp.TaskpoolScheduler)
            //    .Where(d => d.FolderId == _folderId)
            //    .Select(d => d.Changes)
            //    .Subscribe(changes =>
            //    {
            //        _sourceMessages.Edit(updater =>
            //        {
            //            foreach (var c in changes)
            //            {
            //                switch (c.State)
            //                {
            //                    case DeltaState.Add:
            //                    case DeltaState.Update:
            //                        updater.AddOrUpdate(c.Entity.Summary());
            //                        break;
            //                    case DeltaState.Remove:
            //                        updater.RemoveKey(c.Entity.Id);
            //                        break;
            //                }
            //            }
            //        });
            //    });

            Messages = new VirtualizingCache<MessageSummary, MessageSummaryViewModel>(
                new SpecificationVirtualizingSource<MessageSummary>(_queryFactory,
                    query => query.MessageSummaries,
                    new RelaySpecification<MessageSummary>(q => q.Where(m => m.FolderId == _folderId).OrderByDescending(m => m.ReceivedDateTime))),
                state => new MessageSummaryViewModel(state, _queryFactory));

            _mailService.MessageChanges
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Where(d => d.FolderId == _folderId)
                .Select(d => d.Changes.Select(e => new DeltaEntity<MessageSummary>(e.State, e.Entity.Summary())).ToArray())
                .Subscribe(changes => Messages.OnSourceChanged(changes))
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

        //private void LoadMessages()
        //{
        //    if (!_isRestored)
        //    {
        //        _isRestored = true;

        //        Observable.Start(() =>
        //        {
        //            using var query = _queryFactory.Connect();
        //            var messages = query.MessageSummaries.ToList(m => m.FolderId == _folderId);
        //            _sourceMessages.Edit(updater => updater.Load(messages));
        //        }, RxApp.TaskpoolScheduler);

        //        CountMessages()
        //            .ObserveOn(RxApp.MainThreadScheduler)
        //            .Subscribe(x =>
        //            {
        //                UnreadCount = x.UnreadCount;
        //                TotalCount = x.TotalCount;
        //            });
        //    }
        //}

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

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
