using Observatory.Core.Models;
using Observatory.Core.ViewModels.Mail;
using Observatory.UI.Virtualizing;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Reactive.Disposables;
using Windows.ApplicationModel.Store;
using Uno.Extensions;
using Uno.Logging;

namespace Observatory.UI.Views.Mail
{
    public sealed partial class MailFolderView : UserControl, IViewFor<MailFolderViewModel>
    {
        public static DependencyProperty ViewModelProperty { get; } =
            DependencyProperty.Register(nameof(ViewModel), typeof(MailFolderViewModel), typeof(MailFolderView), new PropertyMetadata(null));

        public static DependencyProperty SelectedMessageProperty { get; } =
            DependencyProperty.Register(nameof(SelectedMessage), typeof(MessageSummaryViewModel), typeof(MailFolderView), new PropertyMetadata(null));

        public static DependencyProperty MessagesProperty { get; } =
            DependencyProperty.Register(nameof(Messages), 
                typeof(VirtualizingList<MessageSummary, MessageSummaryViewModel, string>), 
                typeof(MailFolderView), new PropertyMetadata(null));

        public MailFolderViewModel ViewModel
        {
            get { return (MailFolderViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public MessageSummaryViewModel SelectedMessage
        {
            get { return (MessageSummaryViewModel)GetValue(SelectedMessageProperty); }
            set { SetValue(SelectedMessageProperty, value); }
        }

        object IViewFor.ViewModel 
        {
            get => ViewModel;
            set => ViewModel = (MailFolderViewModel)value;
        }

        public VirtualizingList<MessageSummary, MessageSummaryViewModel, string> Messages
        {
            get { return (VirtualizingList<MessageSummary, MessageSummaryViewModel, string>)GetValue(MessagesProperty); }
            set { SetValue(MessagesProperty, value); }
        }

        public MailFolderView()
        {
            this.InitializeComponent();
            FolderNameShadow.Receivers.Add(MessageListGrid);

            this.WhenActivated(disposables =>
            {
                this.WhenAnyValue(x => x.ViewModel.Messages)
                    .WithLatestFrom(this.WhenAnyValue(x => x.ViewModel), (messages, vm) => (Messages: messages, ViewModel: vm))
                    .Where(x => x.ViewModel != null && x.Messages != null)
                    .Select(x => new VirtualizingList<MessageSummary, MessageSummaryViewModel, string>(x.Messages, x.ViewModel.Transform))
                    .BindTo(this, x => x.Messages)
                    .DisposeWith(disposables);

                this.WhenAnyValue(x => x.Messages)
                    .Buffer(2, 1)
                    .Select(x => (Previous: x[0], Current: x[1]))
                    .Subscribe(x =>
                    {
                        x.Previous?.Dispose();
                    })
                    .DisposeWith(disposables);
            });
        }
    }
}
