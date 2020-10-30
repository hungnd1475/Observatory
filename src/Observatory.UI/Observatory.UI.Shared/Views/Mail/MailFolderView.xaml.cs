using Observatory.Core.Models;
using Observatory.Core.ViewModels.Mail;
using Observatory.UI.Virtualizing;
using ReactiveUI;
using System.Reactive.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Observatory.UI.Views.Mail
{
    public sealed partial class MailFolderView : UserControl
    {
        public static DependencyProperty FolderProperty { get; } =
            DependencyProperty.Register(nameof(Folder), typeof(MailFolderViewModel), typeof(MailFolderView), new PropertyMetadata(null));

        public static DependencyProperty SelectedMessageProperty { get; } =
            DependencyProperty.Register(nameof(SelectedMessage), typeof(MessageSummaryViewModel), typeof(MailFolderView), new PropertyMetadata(null));

        public static DependencyProperty MessagesProperty { get; } =
            DependencyProperty.Register(nameof(Messages), 
                typeof(VirtualizingList<MessageSummary, MessageSummaryViewModel>), 
                typeof(MailFolderView), new PropertyMetadata(null));

        public MailFolderViewModel Folder
        {
            get { return (MailFolderViewModel)GetValue(FolderProperty); }
            set { SetValue(FolderProperty, value); }
        }

        public MessageSummaryViewModel SelectedMessage
        {
            get { return (MessageSummaryViewModel)GetValue(SelectedMessageProperty); }
            set { SetValue(SelectedMessageProperty, value); }
        }

        public VirtualizingList<MessageSummary, MessageSummaryViewModel> Messages
        {
            get { return (VirtualizingList<MessageSummary, MessageSummaryViewModel>)GetValue(MessagesProperty); }
            set { SetValue(MessagesProperty, value); }
        }

        public MailFolderView()
        {
            this.InitializeComponent();
            this.WhenAnyValue(x => x.Folder)
                .Where(f => f != null)
                .SelectMany(f => f.WhenAnyValue(x => x.Messages))
                .Select(m => new VirtualizingList<MessageSummary, MessageSummaryViewModel>(m))
                .BindTo(this, x => x.Messages);
        }
    }
}
