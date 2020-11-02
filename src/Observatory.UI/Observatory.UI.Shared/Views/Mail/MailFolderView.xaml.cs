using Observatory.Core.Models;
using Observatory.Core.ViewModels.Mail;
using Observatory.UI.Virtualizing;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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
            this.WhenActivated(disposables =>
            {
                this.OneWayBind(ViewModel, 
                        vm => vm.Messages, 
                        v => v._messageList.ItemsSource, 
                        cache => new VirtualizingList<MessageSummary, MessageSummaryViewModel>(cache))
                    .DisposeWith(disposables);
            });
        }
    }
}
