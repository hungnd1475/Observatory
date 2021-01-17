using DynamicData;
using DynamicData.Binding;
using Observatory.Core.Interactivity;
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

        public string Id { get; }

        public string ParentId { get; }

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

        public MessageListViewModel Messages { get; }

        public ReactiveCommand<Unit, Unit> Synchronize { get; }

        public ReactiveCommand<Unit, Unit> CancelSynchronization { get; }

        [ObservableAsProperty]
        public bool IsSynchronizing { get; }

        public ReactiveCommand<Unit, Unit> Rename { get; }

        public ReactiveCommand<Unit, Unit> Move { get; }

        public ReactiveCommand<Unit, Unit> Delete { get; }

        public ViewModelActivator Activator { get; } = new ViewModelActivator();

        public MailFolderViewModel(Node<MailFolder, string> node,
            MailBoxViewModel mailBox,
            IProfileDataQueryFactory queryFactory,
            IMailService mailService)
        {
            Id = node.Item.Id;
            ParentId = node.Item.ParentId;
            Name = node.Item.Name;
            Type = node.Item.Type;
            IsFavorite = node.Item.IsFavorite;
            Messages = new MessageListViewModel(node.Item.Id, node.Item.Type,
                mailBox, queryFactory, mailService, Activator);

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

            Move = ReactiveCommand.CreateFromTask(async () =>
            {
                var result = await mailBox.PromptUserToSelectFolder(
                    "Move a folder",
                    "Select another folder to move to:",
                    includeRoot: true,
                    CanMoveTo);
                this.Log().Debug(result);
            });

            node.Children.Connect()
                .Transform(n => new MailFolderViewModel(n, mailBox, queryFactory, mailService))
                .Sort(SortExpressionComparer<MailFolderViewModel>.Ascending(f => f.Name))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _childFolders)
                .DisposeMany()
                .Subscribe()
                .DisposeWith(_disposables);

            mailService.MessageChanges
                .Where(changes => changes.AffectsFolder(node.Item.Id))
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
        }

        private bool CanMoveTo(MailFolderSelectionItem destinationFolder)
        {
            return destinationFolder != null &&
                destinationFolder.Id != Id &&
                destinationFolder.Id != ParentId;
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
                var unreadCount = query.MessageSummaries.Count(m => m.FolderId == folderId && !m.IsRead);
                var totalCount = query.MessageSummaries.Count(m => m.FolderId == folderId);
                return (UnreadCount: unreadCount, TotalCount: totalCount);
            },
            RxApp.TaskpoolScheduler);
        }
    }
}
