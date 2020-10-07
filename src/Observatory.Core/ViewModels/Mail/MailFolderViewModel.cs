using DynamicData;
using Observatory.Core.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;

namespace Observatory.Core.ViewModels.Mail
{
    public class MailFolderViewModel : ReactiveObject, IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly ReadOnlyObservableCollection<MailFolderViewModel> _childFolders;

        [Reactive]
        public string Name { get; private set; }

        [ObservableAsProperty]
        public int UnreadCount { get; }

        [ObservableAsProperty]
        public int TotalCount { get; }

        public FolderType Type { get; }

        [Reactive]
        public bool IsFavorite { get; set; }

        [Reactive]
        public bool IsSelected { get; set; }

        public ReadOnlyObservableCollection<MailFolderViewModel> ChildFolders => _childFolders;

        public ReadOnlyObservableCollection<MessageSummaryViewModel> Messages => throw new NotImplementedException();

        public ReactiveCommand<Unit, Unit> SynchronizeCommand => throw new NotImplementedException();

        public ReactiveCommand<Unit, Unit> CancelSynchronizationCommand => throw new NotImplementedException();

        public bool IsSynchronizing => throw new NotImplementedException();

        public ReactiveCommand<Unit, Unit> RenameCommand => throw new NotImplementedException();

        public ReactiveCommand<string, Unit> MoveCommand => throw new NotImplementedException();

        public ReactiveCommand<Unit, Unit> DeleteCommand => throw new NotImplementedException();

        public MailFolderViewModel(Node<MailFolder, string> node)
        {
            node.Children.Connect()
                .Transform(n => new MailFolderViewModel(n))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _childFolders)
                .DisposeMany()
                .Subscribe()
                .DisposeWith(_disposables);
            Name = node.Item.Name;
            Type = node.Item.Type;
            IsFavorite = node.Item.IsFavorite;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
