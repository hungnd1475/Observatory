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

        //[Reactive]
        //public int UnreadCount { get; private set; }

        //[Reactive]
        //public int TotalCount { get; private set; }

        [Reactive]
        public int MessageCount { get; private set; }

        public FolderType Type { get; }

        [Reactive]
        public bool IsFavorite { get; private set; }

        public ReadOnlyObservableCollection<MailFolderViewModel> ChildFolders => _childFolders;

        public MessageListViewModel Messages { get; }

        public ReactiveCommand<Unit, Unit> Synchronize { get; }

        public ReactiveCommand<Unit, Unit> CancelSynchronization { get; }

        public ReactiveCommand<Unit, Unit> Rename { get; }

        public ReactiveCommand<Unit, Unit> Move { get; }

        public ReactiveCommand<Unit, Unit> Delete { get; }

        [ObservableAsProperty]
        public bool IsBusy { get; }

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
                .SelectMany(_ => CountMessages(queryFactory))
                .Subscribe()
                .DisposeWith(_disposables);

            CountMessages(queryFactory)
                .Subscribe()
                .DisposeWith(_disposables);

            Observable.CombineLatest(new[]
                {
                    Synchronize.IsExecuting,
                    this.WhenAnyValue(x => x.Messages.IsBusy),
                },
                x => x.Any(x => x))
            .ToPropertyEx(this, x => x.IsBusy)
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

        public IObservable<Unit> CountMessages(IProfileDataQueryFactory queryFactory)
        {
            return Observable.Start(() =>
            {
                using var query = queryFactory.Connect();
                switch (Type)
                {
                    case FolderType.Archive:
                    case FolderType.DeletedItems:
                    case FolderType.SentItems:
                        return 0;
                    case FolderType.Drafts:
                        return query.MessageSummaries.Count(m => m.FolderId == Id);
                    default:
                        return query.MessageSummaries.Count(m => m.FolderId == Id && !m.IsRead);
                }
            }, RxApp.TaskpoolScheduler)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(count => MessageCount = count)
            .Select(_ => Unit.Default);
        }

        //private static IObservable<(int UnreadCount, int TotalCount)> CountMessages(
        //    IProfileDataQueryFactory queryFactory, string folderId)
        //{
        //    return Observable.Start(() =>
        //    {
        //        using var query = queryFactory.Connect();
        //        var unreadCount = query.MessageSummaries.Count(m => m.FolderId == folderId && !m.IsRead);
        //        var totalCount = query.MessageSummaries.Count(m => m.FolderId == folderId);
        //        return (UnreadCount: unreadCount, TotalCount: totalCount);
        //    },
        //    RxApp.TaskpoolScheduler);
        //}
    }
}
