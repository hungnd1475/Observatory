﻿using Observatory.Core.Interactivity;
using Observatory.Core.Models;
using Observatory.Core.Persistence;
using Observatory.Core.Persistence.Specifications;
using Observatory.Core.Services;
using Observatory.Core.Services.ChangeTracking;
using Observatory.Core.Virtualization;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Observatory.Core.ViewModels.Mail
{
    public class MessageListViewModel : ReactiveObject, IActivatableViewModel
    {
        private readonly string _folderId;
        private readonly IConnectableObservable<bool> _canOperationExecuted;

        [Reactive]
        public MessageOrder Order { get; set; } = MessageOrder.ReceivedDateTime;

        [Reactive]
        public MessageFilter Filter { get; set; } = MessageFilter.None;

        [Reactive]
        public VirtualizingCache<MessageSummary, MessageSummaryViewModel, string> Cache { get; private set; }

        [Reactive]
        public bool IsSelecting { get; set; } = false;

        [Reactive]
        public int SelectionCount { get; set; }

        public ReactiveCommand<IReadOnlyList<string>, Unit> Archive { get; }

        public ReactiveCommand<IReadOnlyList<string>, Unit> Delete { get; }

        public ReactiveCommand<IReadOnlyList<string>, Unit> SetFlag { get; }

        public ReactiveCommand<IReadOnlyList<string>, Unit> ClearFlag { get; }

        public ReactiveCommand<IReadOnlyList<string>, Unit> MarkAsRead { get; }

        public ReactiveCommand<IReadOnlyList<string>, Unit> MarkAsUnread { get; }

        public ReactiveCommand<IReadOnlyList<string>, Unit> Move { get; }

        public ReactiveCommand<IReadOnlyList<string>, Unit> MoveToJunk { get; }

        public ViewModelActivator Activator { get; }

        public MessageListViewModel(string folderId,
            MailBoxViewModel mailBox,
            IProfileDataQueryFactory queryFactory,
            IMailService mailService,
            ViewModelActivator activator)
        {
            _folderId = folderId;
            Activator = activator;

            _canOperationExecuted = this.WhenAnyObservable(x => x.Cache.SelectionChanged)
                .Select(x => x.Sum(r => r.Length) > 0)
                .Publish();

            MarkAsRead = ReactiveCommand.Create<IReadOnlyList<string>>(async messageIds =>
            {
                PrepareMessageIds(ref messageIds);
                await mailService.UpdateMessage(messageIds)
                    .Set(m => m.IsRead, true)
                    .ExecuteAsync();
            }, _canOperationExecuted);

            MarkAsUnread = ReactiveCommand.Create<IReadOnlyList<string>>(async messageIds =>
            {
                PrepareMessageIds(ref messageIds);
                await mailService.UpdateMessage(messageIds)
                    .Set(m => m.IsRead, false)
                    .ExecuteAsync();
            }, _canOperationExecuted);

            SetFlag = ReactiveCommand.Create<IReadOnlyList<string>>(async messageIds =>
            {
                PrepareMessageIds(ref messageIds);
                await mailService.UpdateMessage(messageIds)
                    .Set(m => m.IsFlagged, true)
                    .ExecuteAsync();
            }, _canOperationExecuted);

            ClearFlag = ReactiveCommand.Create<IReadOnlyList<string>>(async messageIds =>
            {
                PrepareMessageIds(ref messageIds);
                await mailService.UpdateMessage(messageIds)
                    .Set(m => m.IsFlagged, false)
                    .ExecuteAsync();
            }, _canOperationExecuted);

            Move = ReactiveCommand.CreateFromTask<IReadOnlyList<string>>(async (messageIds) =>
            {
                PrepareMessageIds(ref messageIds);
                var result = await mailBox.PromptUserToSelectFolder(
                    messageIds.Count == 1 ? "Move a message" : $"Move {messageIds.Count} messages",
                    "Select another folder to move to:",
                    includeRoot: false,
                    CanMoveTo);
                this.Log().Debug(result);
            }, _canOperationExecuted);

            this.WhenActivated(disposables =>
            {
                _canOperationExecuted.Connect()
                    .DisposeWith(disposables);

                Observable.CombineLatest(
                    this.WhenAnyValue(x => x.Order),
                    this.WhenAnyValue(x => x.Filter),
                    (order, filter) => (Order: order, Filter: filter))
                .DistinctUntilChanged()
                .Where(x => x.Order != MessageOrder.Sender)
                .Subscribe(x =>
                {
                    Cache?.Dispose();
                    Cache = new VirtualizingCache<MessageSummary, MessageSummaryViewModel, string>(
                        new PersistentVirtualizingSource<MessageSummary, string>(queryFactory,
                            GetItemSpecification(_folderId, x.Order, x.Filter),
                            GetIndexSpecification(_folderId, x.Order, x.Filter)),
                        mailService.MessageChanges
                            .Select(changes => FilterChanges(changes.ForFolder(folderId), x.Filter)),
                        state => new MessageSummaryViewModel(state, this, queryFactory));
                })
                .DisposeWith(disposables);

                this.WhenAnyObservable(x => x.Cache.SelectionChanged)
                    .Select(ranges => ranges.Sum(r => r.Length))
                    .Do(x => SelectionCount = x)
                    .Subscribe()
                    .DisposeWith(disposables);

                Disposable.Create(() =>
                {
                    Filter = MessageFilter.None;
                    Cache?.Dispose();
                    Cache = null;
                    IsSelecting = false;
                    SelectionCount = 0;
                })
                .DisposeWith(disposables);
            });
        }

        private bool CanMoveTo(MailFolderSelectionItem destinationFolder)
        {
            return destinationFolder != null &&
                destinationFolder.Id != _folderId;
        }

        private void PrepareMessageIds(ref IReadOnlyList<string> messageIds)
        {
            if (messageIds == null || messageIds.Count == 0)
            {
                messageIds = Cache.GetSelectedKeys();
            }
        }

        public static DeltaEntity<MessageSummary>[] FilterChanges(
            IEnumerable<DeltaEntity<Message>> changes,
            MessageFilter filter)
        {
            var filteredChanges = filter switch
            {
                MessageFilter.None => changes,
                MessageFilter.Unread => changes.Where(f => !f.Entity.IsRead),
                MessageFilter.Flagged => changes.Where(f => f.Entity.IsFlagged),
                _ => throw new NotSupportedException(),
            };
            return filteredChanges
                .Select(e => new DeltaEntity<MessageSummary>(e.State, e.Entity.Summary()))
                .ToArray();
        }

        private static ISpecification<MessageSummary, MessageSummary> GetItemSpecification(
            string folderId, MessageOrder order, MessageFilter filter)
        {
            var specification = Specification.Relay<MessageSummary>(q => q.Where(m => m.FolderId == folderId));
            switch (filter)
            {
                case MessageFilter.Unread:
                    specification = specification.Chain(q => q.Where(m => !m.IsRead));
                    break;
                case MessageFilter.Flagged:
                    specification = specification.Chain(q => q.Where(m => m.IsFlagged));
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
                        specification = specification.Chain(q => q.Where(m => !m.IsRead));
                        break;
                    case MessageFilter.Flagged:
                        specification = specification.Chain(q => q.Where(m => m.IsFlagged));
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
