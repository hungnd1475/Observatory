using DynamicData;
using DynamicData.Binding;
using Observatory.Core.Models;
using Observatory.Core.ViewModels.Mail;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;

namespace Observatory.Core.Interactivity
{
    public static partial class Interactions
    {
        public static Interaction<MailFolderSelectionViewModel, MailFolderSelectionResult> MailFolderSelection { get; } =
            new Interaction<MailFolderSelectionViewModel, MailFolderSelectionResult>();
    }

    public class MailFolderSelectionViewModel : ReactiveObject, IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly ReadOnlyObservableCollection<MailFolderSelectionItem> _folders;

        [Reactive]
        public string TitleText { get; set; }

        [Reactive]
        public string PromptText { get; set; }

        [Reactive]
        public bool IncludeRoot { get; set; }

        [Reactive]
        public Func<MailFolderSelectionItem, bool> CanMoveTo { get; set; }

        public ReadOnlyObservableCollection<MailFolderSelectionItem> Folders => _folders;

        public MailFolderSelectionViewModel(IObservableCache<MailFolder, string> folders)
        {
            folders.Connect()
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Filter(this.WhenAnyValue(x => x.IncludeRoot).Select<bool, Func<MailFolder, bool>>(
                    includeRoot => f => includeRoot || f.Type != FolderType.Root))
                .Sort(SortExpressionComparer<MailFolder>.Ascending(f => f.Type).ThenByAscending(f => f.Name))
                .TransformToTree(f => f.ParentId)
                .Transform(n => new MailFolderSelectionItem(n))
                .DisposeMany()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _folders)
                .Subscribe()
                .DisposeWith(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }

    public class MailFolderSelectionItem : ReactiveObject, IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly ReadOnlyObservableCollection<MailFolderSelectionItem> _childFolders;

        public string Id { get; }

        public string ParentId { get; }

        [Reactive]
        public string Name { get; set; }

        public FolderType Type { get; }

        public ReadOnlyObservableCollection<MailFolderSelectionItem> ChildFolders => _childFolders;

        public MailFolderSelectionItem(Node<MailFolder, string> node)
        {
            Id = node.Item.Id;
            ParentId = node.Item.ParentId;
            Name = node.Item.Type == FolderType.Root ? "Folders" : node.Item.Name;
            Type = node.Item.Type;
            node.Children.Connect()
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Transform(n => new MailFolderSelectionItem(n))
                .Sort(SortExpressionComparer<MailFolderSelectionItem>.Ascending(f => f.Type).ThenByAscending(f => f.Name))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _childFolders)
                .DisposeMany()
                .Subscribe()
                .DisposeWith(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }

    public struct MailFolderSelectionResult
    {
        public bool IsCancelled { get; }
        public MailFolderSelectionItem DestinationFolder { get; }

        public MailFolderSelectionResult(bool isCancelled,
            MailFolderSelectionItem destinationFolder)
        {
            IsCancelled = isCancelled;
            DestinationFolder = destinationFolder;
        }

        public override string ToString()
        {
            return IsCancelled ? $"{nameof(MailFolderSelectionResult)} {{ {nameof(IsCancelled)} = {IsCancelled} }}"
                : $"{nameof(MailFolderSelectionResult)} {{ {nameof(DestinationFolder)} = '{DestinationFolder.Id}' }}";
        }
    }
}
