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
using System.Reactive.Subjects;

namespace Observatory.Core.ViewModels.Mail
{
    public class MailFolderViewModel : ReactiveObject, IActivatableViewModel, IDisposable
    {
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
        public MessageOrder MessageOrder { get; set; } = MessageOrder.ReceivedDateTime;

        [Reactive]
        public MessageFilter MessageFilter { get; set; } = MessageFilter.None;

        [Reactive]
        public VirtualizingCache<MessageSummary, MessageSummaryViewModel, string> Messages { get; private set; }

        public ReactiveCommand<Unit, Unit> Synchronize { get; }

        public ReactiveCommand<Unit, Unit> CancelSynchronization { get; }

        [ObservableAsProperty]
        public bool IsSynchronizing { get; }

        public ReactiveCommand<Unit, Unit> Rename { get; }

        public ReactiveCommand<string, Unit> Move { get; }

        public ReactiveCommand<Unit, Unit> Delete { get; }

        public ViewModelActivator Activator { get; } = new ViewModelActivator();

        public MailFolderViewModel(Node<MailFolder, string> node,
            IProfileDataQueryFactory queryFactory,
            IMailService mailService)
        {
            Name = node.Item.Name;
            Type = node.Item.Type;
            IsFavorite = node.Item.IsFavorite;

            Synchronize = ReactiveCommand.CreateFromObservable(() => Observable
                .StartAsync((token) => mailService.SynchronizeMessagesAsync(node.Item.Id, token))
                .TakeUntil(CancelSynchronization));
            Synchronize.IsExecuting
                .ToPropertyEx(this, x => x.IsSynchronizing)
                .DisposeWith(_disposables);
            Synchronize.ThrownExceptions
                .Subscribe(ex => this.Log().Error(ex))
                .DisposeWith(_disposables);
            CancelSynchronization = ReactiveCommand.Create(() => { }, Synchronize.IsExecuting);

            node.Children.Connect()
                .Transform(n => new MailFolderViewModel(n, queryFactory, mailService))
                .Sort(SortExpressionComparer<MailFolderViewModel>.Ascending(f => f.Name))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _childFolders)
                .DisposeMany()
                .Subscribe()
                .DisposeWith(_disposables);

            mailService.MessageChanges
                .Where(d => d.FolderId == node.Item.Id)
                .SelectMany(_ => CountMessages(queryFactory, node.Item.Id))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    UnreadCount = x.UnreadCount;
                    TotalCount = x.TotalCount;
                })
                .DisposeWith(_disposables);

            CountMessages(queryFactory, node.Item.Id)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    UnreadCount = x.UnreadCount;
                    TotalCount = x.TotalCount;
                })
                .DisposeWith(_disposables);

            this.WhenActivated(disposables =>
            {
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.MessageOrder),
                        this.WhenAnyValue(x => x.MessageFilter),
                        (order, filter) => (Order: order, Filter: filter))
                    .DistinctUntilChanged()
                    .Where(x => x.Order != MessageOrder.Sender)
                    .Subscribe(x =>
                    {
                        Messages?.Dispose();
                        Messages = new VirtualizingCache<MessageSummary, MessageSummaryViewModel, string>(
                            new PersistentVirtualizingSource<MessageSummary, string>(queryFactory,
                                GetItemSpecification(node.Item.Id, x.Order, x.Filter),
                                GetIndexSpecification(node.Item.Id, x.Order, x.Filter)),
                            mailService.MessageChanges
                                .Where(d => d.FolderId == node.Item.Id)
                                .Select(d => d.Changes.Select(e => new DeltaEntity<MessageSummary>(e.State, e.Entity.Summary())).ToArray()),
                            state => new MessageSummaryViewModel(state, queryFactory, mailService));
                    })
                    .DisposeWith(disposables);

                Disposable.Create(() =>
                {
                    MessageFilter = MessageFilter.None;
                    Messages?.Dispose();
                    Messages = null;
                })
                .DisposeWith(disposables);
            });
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        private static IObservable<(int UnreadCount, int TotalCount)> CountMessages(
            IProfileDataQueryFactory queryFactory, string folderId)
        {
            return Observable.Start(() =>
            {
                using var query = queryFactory.Connect();
                var unreadCount = query.MessageSummaries.Count(m => m.FolderId == folderId && !m.IsRead.Value);
                var totalCount = query.MessageSummaries.Count(m => m.FolderId == folderId);
                return (UnreadCount: unreadCount, TotalCount: totalCount);
            },
            RxApp.TaskpoolScheduler);
        }

        private static ISpecification<MessageSummary, MessageSummary> GetItemSpecification(
            string folderId, MessageOrder order, MessageFilter filter)
        {
            var specification = Specification.Relay<MessageSummary>(q => q.Where(m => m.FolderId == folderId));
            switch (filter)
            {
                case MessageFilter.Unread:
                    specification = specification.Chain(q => q.Where(m => !m.IsRead.Value));
                    break;
                case MessageFilter.Flagged:
                    specification = specification.Chain(q => q.Where(m => m.IsFlagged.Value));
                    break;
            }

            switch (order)
            {
                case MessageOrder.ReceivedDateTime:
                    specification = specification.Chain(q => q
                        .OrderByDescending(m => m.ReceivedDateTime)
                        .ThenBy(m => m.Id));
                    break;
                case MessageOrder.Sender:
                    break;
            }
            return specification;
        }

        private static Func<MessageSummary, ISpecification<MessageSummary, MessageSummary>> GetIndexSpecification(
            string folderId, MessageOrder order, MessageFilter filter)
        {
            return (entity) =>
            {
                var specification = Specification.Relay<MessageSummary>(q => q.Where(m => m.FolderId == folderId));
                switch (filter)
                {
                    case MessageFilter.Unread:
                        specification = specification.Chain(q => q.Where(m => !m.IsRead.Value));
                        break;
                    case MessageFilter.Flagged:
                        specification = specification.Chain(q => q.Where(m => m.IsFlagged.Value));
                        break;
                }
                switch (order)
                {
                    case MessageOrder.ReceivedDateTime:
                        specification = specification.Chain(q => q.Where(m => m.ReceivedDateTime > entity.ReceivedDateTime
                            || m.ReceivedDateTime == entity.ReceivedDateTime && string.Compare(m.Id, entity.Id) < 0));
                        break;
                    case MessageOrder.Sender:
                        break;
                }
                return specification;
            };
        }
    }

    public enum MessageOrder
    {
        ReceivedDateTime,
        Sender,
    }

    public enum MessageFilter
    {
        None,
        Unread,
        Flagged,
    }
}
