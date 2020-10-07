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

namespace Observatory.Application.ViewModels.Mail
{
    public class MailFolderViewModel : ReactiveObject, IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly ReadOnlyObservableCollection<MailFolderViewModel> _childFolders;

        [Reactive]
        public string DisplayName { get; private set; }

        [ObservableAsProperty]
        public int UnreadCount { get; }

        [ObservableAsProperty]
        public int TotalCount { get; }

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
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
