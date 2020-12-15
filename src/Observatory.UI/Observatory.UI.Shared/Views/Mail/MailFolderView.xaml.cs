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

namespace Observatory.UI.Views.Mail
{
    public sealed partial class MailFolderView : UserControl, IViewFor<MailFolderViewModel>
    {
        public static DependencyProperty ViewModelProperty { get; } =
            DependencyProperty.Register(nameof(ViewModel), typeof(MailFolderViewModel), typeof(MailFolderView), new PropertyMetadata(null));

        public static DependencyProperty SelectedMessageProperty { get; } =
            DependencyProperty.Register(nameof(SelectedMessage), typeof(MessageSummaryViewModel), typeof(MailFolderView), new PropertyMetadata(null));

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

        public MailFolderView()
        {
            this.InitializeComponent();
            FolderNameShadow.Receivers.Add(MessageListGrid);

            this.WhenActivated(disposables =>
            {
                this.WhenAnyValue(x => x.ViewModel)
                    .Where(vm => vm != null)
                    .SelectMany(vm => vm.WhenAnyValue(x => x.Messages).Where(m => m != null).Select(messages => (ViewModel: vm, Messages: messages)))
                    .Select(x => new VirtualizingList<MessageSummary, MessageSummaryViewModel, string>(x.Messages, x.ViewModel.Transform))
                    .Subscribe(source =>
                    {
                        (_messageList.ItemsSource as IDisposable)?.Dispose();
                        _messageList.ItemsSource = source;
                    })
                    .DisposeWith(disposables);
            });
        }
    }
}
